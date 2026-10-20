using Archive.Core.Processing;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Archive.Core.Entities;
using Archive.Core.Jobs;
using Archive.Core.Options;
using Archive.Core.Storage;
using Archive.Worker.Capture;
using Archive.Worker.Capture.Browser;
using Archive.Worker.Storage;
using Microsoft.Playwright;
using Archive.Core.Entities.Processing;

namespace Archive.Worker.Import;

public sealed class OfflineImportEngine(
    IEnumerable<IUploadedDocumentDecoder> decoders,
    IArchiveObjectStore objectStore,
    ArchiveOptions options,
    ArchiveBrowserOptions browserOptions,
    ILogger<OfflineImportEngine> logger)
{
    private readonly IReadOnlyDictionary<SourceType, IUploadedDocumentDecoder> _decoders = BuildDecoders(decoders);
    private readonly CaptureLimits _captureLimits = options.ToCaptureLimits();
    private readonly ImportLimits _importLimits = new(
        options.MaxUploadBytes, options.MaxResources, options.MaxImportDepth, options.MaxImportObjects);

    private static IReadOnlyDictionary<SourceType, IUploadedDocumentDecoder> BuildDecoders(
        IEnumerable<IUploadedDocumentDecoder> decoders)
    {
        var values = decoders.ToArray();
        if (values.Select(x => x.SourceType).Distinct().Count() != values.Length ||
            !new[] { SourceType.HtmlFile, SourceType.MhtmlFile, SourceType.WebArchiveFile }
                .All(type => values.Any(decoder => decoder.SourceType == type)))
            throw new InvalidOperationException("A unique decoder is required for each supported upload format.");
        return values.ToDictionary(x => x.SourceType);
    }

    public async Task<CaptureResult> CaptureAsync(CaptureInput job, IProgress<ProgressStep>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(job.UploadedObjectKey) ||
            !_decoders.TryGetValue(job.SourceType, out var decoder))
            throw new CaptureFailedException("Für diesen Datei-Upload ist kein Importadapter registriert.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_captureLimits.MaxDuration);
        var timer = Stopwatch.StartNew();
        try
        {
            progress?.Report(ProgressStep.SourceCheck);
            var source = await objectStore.GetAsync(job.UploadedObjectKey, timeout.Token)
                ?? throw new CaptureFailedException("Die hochgeladene Datei konnte nicht gelesen werden.");
            byte[] original;
            await using (source.Content)
                original = await ImportStream.ReadAllBytesAsync(source.Content, _importLimits.MaxBytes, timeout.Token);
            await using var input = new MemoryStream(original, writable: false);
            ImportedPage imported;
            try
            {
                imported = await decoder.DecodeAsync(input, _importLimits, timeout.Token);
            }
            catch (InvalidDataException exception)
            {
                throw new CaptureFailedException("Die hochgeladene Datei ist ungültig oder überschreitet ein Importlimit.",
                    exception);
            }
            progress?.Report(ProgressStep.LoadingPage);
            var normalized = OfflinePageNormalizer.Normalize(imported);
            if (normalized.Assets.Count > _importLimits.MaxResources + _importLimits.MaxObjects)
                throw new CaptureDiscardedException("Die Datei enthält zu viele eingebettete Inhalte.");

            progress?.Report(ProgressStep.LoadingDynamicContent);
            var screenshot = await ScreenshotAsync(normalized, timeout.Token);
            progress?.Report(ProgressStep.SavingSnapshot);
            var result = await StoreAsync(job, imported, normalized, screenshot.Bytes, original,
                screenshot.MissingUrls, timer.ElapsedMilliseconds, timeout.Token);
            return result;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new CaptureCancelledException();
        }
        catch (Exception) when (timeout.IsCancellationRequested)
        {
            throw new CaptureDiscardedException("Das Zeitlimit für den Dateiimport wurde überschritten.");
        }
        catch (CaptureFailedException)
        {
            throw;
        }
        catch (CaptureDiscardedException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Dateiimport für Job {JobId} fehlgeschlagen", job.Id);
            throw new CaptureFailedException("Die Datei konnte nicht archiviert werden.", exception);
        }
    }

    private async Task<(byte[] Bytes, IReadOnlyList<string> MissingUrls)> ScreenshotAsync(NormalizedPage normalized,
        CancellationToken cancellationToken)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = browserOptions.Headless,
            Channel = browserOptions.Headless ? "chromium" : null,
            Timeout = (float)_captureLimits.MaxDuration.TotalMilliseconds
        });
        using var registration = cancellationToken.Register(() => _ = browser.CloseAsync());
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            JavaScriptEnabled = false,
            ServiceWorkers = ServiceWorkerPolicy.Block,
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            Locale = "de-DE"
        });
        var assets = normalized.Assets.ToDictionary(x => "/" + x.Path, StringComparer.Ordinal);
        var missing = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        await context.RouteAsync("**/*", async route =>
        {
            if (Uri.TryCreate(route.Request.Url, UriKind.Absolute, out var uri) &&
                uri.Host == "archive.local" && assets.TryGetValue(uri.AbsolutePath, out var asset))
            {
                await route.FulfillAsync(new RouteFulfillOptions
                {
                    Status = 200,
                    ContentType = asset.ContentType,
                    BodyBytes = asset.Body
                });
            }
            else
            {
                missing.TryAdd(route.Request.Url, 0);
                await route.AbortAsync();
            }
        });
        var page = await context.NewPageAsync();
        var html = normalized.Html.Replace("<base href=\"./\">",
            "<base href=\"http://archive.local/\">", StringComparison.Ordinal);
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.Load })
            .WaitAsync(cancellationToken);
        var bytes = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = false,
            Type = ScreenshotType.Png
        }).WaitAsync(cancellationToken);
        return (bytes, missing.Keys.Order(StringComparer.Ordinal).ToArray());
    }

    private async Task<CaptureResult> StoreAsync(CaptureInput job, ImportedPage imported,
        NormalizedPage normalized, byte[] screenshot, byte[] original, IReadOnlyList<string> missingUrls,
        long durationMilliseconds, CancellationToken cancellationToken)
    {
        var writer = new SnapshotObjectWriter(objectStore, job.Id, _captureLimits.MaxStorageBytes, logger);

        try
        {
            await writer.WriteAsync("index.html", Encoding.UTF8.GetBytes(normalized.Html), "text/html; charset=utf-8", cancellationToken);
            await writer.WriteAsync("screenshot.png", screenshot, "image/png", cancellationToken);
            foreach (var asset in normalized.Assets)
                await writer.WriteAsync(asset.Path, asset.Body, asset.ContentType, cancellationToken);
            var extension = job.SourceType switch
            {
                SourceType.HtmlFile => ".html",
                SourceType.MhtmlFile => ".mhtml",
                SourceType.WebArchiveFile => ".webarchive",
                _ => throw new InvalidOperationException("Unsupported upload type.")
            };
            await writer.WriteAsync("source" + extension, original, "application/octet-stream", cancellationToken);
            var manifest = new
            {
                version = 2,
                resourceReferenceMode = "relative-v1",
                sourceType = job.SourceType.ToString(),
                originalUrl = imported.BaseUrl ?? job.OriginUrl,
                sourceSizeBytes = original.LongLength,
                sourceSha256 = Convert.ToHexString(SHA256.HashData(original)).ToLowerInvariant(),
                htmlObjectKey = writer.GetObjectKey("index.html"),
                screenshotObjectKey = writer.GetObjectKey("screenshot.png"),
                sourceObjectKey = writer.GetObjectKey("source" + extension),
                resources = normalized.Assets.Select(x => new
                {
                    url = x.SourceUrl,
                    contentType = x.ContentType,
                    objectKey = writer.GetObjectKey(x.Path)
                }),
                missingUrls = missingUrls.Take(100).ToArray(),
                missingUrlCount = missingUrls.Count,
                incomplete = missingUrls.Count != 0
            };
            await writer.WriteManifestAsync(manifest, null, cancellationToken);
            return new CaptureResult(missingUrls.Count != 0 ? SnapshotQuality.Incomplete : SnapshotQuality.Complete,
                writer.ContentPrefix, writer.StorageBytes, normalized.Assets.Count, durationMilliseconds, writer.StoredObjectKeys);
        }
        catch
        {
            await writer.RollbackAsync();
            throw;
        }
    }
}
