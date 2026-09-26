using System.Diagnostics;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Archive.Core.Entities;
using Archive.Core.Jobs;
using Archive.Core.Storage;
using Archive.Worker.Capture;
using Microsoft.Playwright;

namespace Archive.Worker.Import;

public sealed class OfflineImportEngine(
    IEnumerable<IUploadedDocumentDecoder> decoders,
    IArchiveObjectStore objectStore,
    IConfiguration configuration,
    ArchiveBrowserOptions browserOptions,
    ILogger<OfflineImportEngine> logger)
{
    private readonly IReadOnlyDictionary<SourceType, IUploadedDocumentDecoder> _decoders = BuildDecoders(decoders);
    private readonly CaptureLimits _captureLimits = CaptureLimits.FromConfiguration(configuration);
    private readonly ImportLimits _importLimits = new(
        configuration.GetValue("Archive:MaxUploadBytes", 250_000_000L),
        configuration.GetValue("Archive:MaxResources", 500),
        configuration.GetValue("Archive:MaxImportDepth", 32),
        configuration.GetValue("Archive:MaxImportObjects", 100_000));

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

    public async Task<CaptureResult> CaptureAsync(ArchiveJob job, IProgress<ProgressUpdate>? progress,
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
            progress?.Report(new ProgressUpdate(ProgressStep.SourceCheck, 10, "Datei wird gelesen"));
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
                throw new CaptureFailedException("Die hochgeladene Datei ist ungültig oder überschreitet ein Importlimit.", exception);
            }
            progress?.Report(new ProgressUpdate(ProgressStep.LoadingPage, 35, "Archivierte Seite wird vorbereitet"));
            var normalized = OfflinePageNormalizer.Normalize(imported, job.Id);
            if (normalized.Assets.Count > _importLimits.MaxResources + _importLimits.MaxObjects)
                throw new CaptureDiscardedException("Die Datei enthält zu viele eingebettete Inhalte.");

            var screenshot = await ScreenshotAsync(normalized, job.Id, timeout.Token);
            progress?.Report(new ProgressUpdate(ProgressStep.LoadingDynamicContent, 65, "Vorschau wurde erstellt"));
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

    private async Task<(byte[] Bytes, IReadOnlyList<string> MissingUrls)> ScreenshotAsync(NormalizedPage normalized, Guid id,
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
        var rootPath = $"/api/snapshots/{id}/content/";
        var assets = normalized.Assets.ToDictionary(x => rootPath + x.Path, StringComparer.Ordinal);
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
        var html = normalized.Html.Replace($"<base href=\"{rootPath}\">",
            $"<base href=\"http://archive.local{rootPath}\">", StringComparison.Ordinal);
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.Load })
            .WaitAsync(cancellationToken);
        var bytes = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = false,
            Type = ScreenshotType.Png
        }).WaitAsync(cancellationToken);
        return (bytes, missing.Keys.Order(StringComparer.Ordinal).ToArray());
    }

    private async Task<CaptureResult> StoreAsync(ArchiveJob job, ImportedPage imported,
        NormalizedPage normalized, byte[] screenshot, byte[] original, IReadOnlyList<string> missingUrls,
        long durationMilliseconds, CancellationToken cancellationToken)
    {
        var prefix = $"snapshots/{job.Id:N}";
        var storedKeys = new List<string>();
        long totalBytes = 0;
        async Task PutAsync(string path, byte[] body, string type)
        {
            if (body.LongLength > _captureLimits.MaxStorageBytes - totalBytes)
                throw new CaptureDiscardedException("Das Speicherlimit wurde überschritten.");
            var key = $"{prefix}/{path}";
            storedKeys.Add(key);
            await using var stream = new MemoryStream(body, writable: false);
            await objectStore.PutAsync(key, stream, type, cancellationToken);
            totalBytes += body.LongLength;
        }

        try
        {
            await PutAsync("index.html", Encoding.UTF8.GetBytes(normalized.Html), "text/html; charset=utf-8");
            await PutAsync("screenshot.png", screenshot, "image/png");
            foreach (var asset in normalized.Assets)
                await PutAsync(asset.Path, asset.Body, asset.ContentType);
            var extension = job.SourceType switch
            {
                SourceType.HtmlFile => ".html",
                SourceType.MhtmlFile => ".mhtml",
                SourceType.WebArchiveFile => ".webarchive",
                _ => throw new InvalidOperationException("Unsupported upload type.")
            };
            await PutAsync("source" + extension, original, "application/octet-stream");
            var manifest = new
            {
                version = 2,
                sourceType = job.SourceType.ToString(),
                originalUrl = imported.BaseUrl ?? job.OriginUrl,
                sourceSizeBytes = original.LongLength,
                sourceSha256 = Convert.ToHexString(SHA256.HashData(original)).ToLowerInvariant(),
                htmlObjectKey = $"{prefix}/index.html",
                screenshotObjectKey = $"{prefix}/screenshot.png",
                sourceObjectKey = $"{prefix}/source{extension}",
                resources = normalized.Assets.Select(x => new
                {
                    url = x.SourceUrl,
                    contentType = x.ContentType,
                    objectKey = $"{prefix}/{x.Path}"
                }),
                missingUrls = missingUrls.Take(100).ToArray(),
                missingUrlCount = missingUrls.Count,
                incomplete = missingUrls.Count != 0
            };
            await PutAsync("manifest.json", JsonSerializer.SerializeToUtf8Bytes(manifest), "application/json");
            return new CaptureResult(missingUrls.Count != 0 ? SnapshotQuality.Incomplete : SnapshotQuality.Complete,
                prefix, totalBytes, normalized.Assets.Count, durationMilliseconds, storedKeys.ToArray());
        }
        catch
        {
            foreach (var key in storedKeys.AsEnumerable().Reverse())
            {
                try { await objectStore.DeleteAsync(key, CancellationToken.None); }
                catch (Exception exception) { logger.LogWarning(exception, "Archivobjekt {ObjectKey} konnte nicht bereinigt werden", key); }
            }
            throw;
        }
    }
}
