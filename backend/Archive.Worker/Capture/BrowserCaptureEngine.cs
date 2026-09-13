using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Archive.Core.Entities;
using Archive.Core.Jobs;
using Archive.Core.Storage;
using Microsoft.Playwright;

namespace Archive.Worker.Capture;

public sealed class BrowserCaptureEngine(IArchiveObjectStore objectStore, IConfiguration configuration,
    ArchiveBrowserOptions browserOptions, WebBotAuthSigner webBotAuthSigner, CrawlPolicyService crawlPolicy,
    ILogger<BrowserCaptureEngine> logger)
{
    private readonly CaptureLimits _limits = CaptureLimits.FromConfiguration(configuration);

    public async Task<CaptureResult> CaptureAsync(ArchiveJob job, IProgress<ProgressUpdate>? progress, CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_limits.MaxDuration);

        try
        {
            timeout.Token.ThrowIfCancellationRequested();
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = browserOptions.Headless,
                Channel = browserOptions.Headless ? "chromium" : null,
                Timeout = (float)_limits.MaxDuration.TotalMilliseconds
            });
            // Playwright operations do not accept CancellationToken. Closing the
            // browser interrupts navigation, evaluation and response body reads too.
            using var cancellationRegistration = timeout.Token.Register(() => _ = CloseBrowserAsync(browser));
            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
                Locale = "de-DE",
                UserAgent = browserOptions.UserAgent,
                ServiceWorkers = ServiceWorkerPolicy.Block
            });
            context.SetDefaultTimeout((float)_limits.MaxDuration.TotalMilliseconds);
            context.SetDefaultNavigationTimeout((float)_limits.MaxDuration.TotalMilliseconds);
            await using var requestRouter = new BrowserRequestRouter(context, webBotAuthSigner);
            await requestRouter.InitializeAsync();

            if (job.SourceType == SourceType.HtmlFile)
            {
                var page = await context.NewPageAsync();
                return await CaptureHtmlAsync(job, page, requestRouter, started, progress, timeout.Token);
            }

            return await CaptureUrlAsync(job, context, requestRouter, started, progress, timeout.Token);
        }
        catch (Exception) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new CaptureDiscardedException("Das Zeitlimit für die Archivierung wurde überschritten.");
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new CaptureCancelledException();
        }
        catch (CaptureDiscardedException)
        {
            throw;
        }
        catch (CaptureFailedException exception)
        {
            logger.LogWarning("Archivierung von Job {JobId} fehlgeschlagen: {Reason}", job.Id, exception.Message);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Archivierung von Job {JobId} fehlgeschlagen", job.Id);
            throw new CaptureFailedException("Die Seite konnte nicht archiviert werden.", exception);
        }
    }

    private async Task<CaptureResult> CaptureUrlAsync(ArchiveJob job, IBrowserContext context,
        BrowserRequestRouter requestRouter, DateTime started,
        IProgress<ProgressUpdate>? progress, CancellationToken cancellationToken)
    {
        if (!UrlNormalizer.TryNormalizeSource(job.OriginUrl, out var normalizedUrl, out var urlError))
            throw new CaptureFailedException(urlError);

        progress?.Report(new ProgressUpdate(ProgressStep.SourceCheck, 5, "Quelle wird geprüft"));
        await crawlPolicy.EnsureAllowedAsync(new Uri(normalizedUrl), cancellationToken);
        var networkGuard = new PublicNetworkGuard();
        var accessPage = await context.NewPageAsync();
        using (var access = new CapturePageAccess(accessPage, normalizedUrl, browserOptions, networkGuard,
                   requestRouter, logger))
        {
            try
            {
                await access.InitializeAsync(cancellationToken);
                await access.NavigateAsync(cancellationToken);
                await access.WaitForAccessAsync(cancellationToken);
                logger.LogInformation("Zugriffsprüfung für Job {JobId} abgeschlossen; beginne saubere Aufnahme", job.Id);
            }
            finally
            {
                await ClosePageAsync(accessPage);
            }
        }

        // A new page isolates late responses from the access phase; the browser
        // context retains Cloudflare-issued cookies without using a user profile.
        for (var attempt = 0; attempt <= browserOptions.ChallengeReloadAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = await context.NewPageAsync();
            using var access = new CapturePageAccess(page, normalizedUrl, browserOptions, networkGuard,
                requestRouter, logger);
            var collector = new CapturedResourceCollector(page, _limits, logger, cancellationToken);
            try
            {
                await access.InitializeAsync(cancellationToken);
                await access.NavigateAsync(cancellationToken);
                await access.VerifyCaptureAsync(cancellationToken);
                progress?.Report(new ProgressUpdate(ProgressStep.LoadingPage, 20, "Seite wird geladen"));
                try
                {
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10_000 })
                        .WaitAsync(cancellationToken);
                }
                catch (TimeoutException)
                {
                    // Analytics and streaming connections need not become idle.
                }
                await access.VerifyCaptureAsync(cancellationToken);
                progress?.Report(new ProgressUpdate(ProgressStep.LoadingDynamicContent, 35, "Dynamische Inhalte werden nachgeladen"));

                await LoadDynamicContentAsync(page, cancellationToken);
                await access.VerifyCaptureAsync(cancellationToken);
                var documentVersion = access.DocumentVersion;
                var html = await CloudflareChallengeDetector.SnapshotHtmlAsync(page, cancellationToken);
                var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions { FullPage = false, Type = ScreenshotType.Png })
                    .WaitAsync(cancellationToken);
                await collector.StopAsync();
                await access.VerifyCaptureAsync(cancellationToken);
                await access.EnsureArchivingAllowedAsync(cancellationToken);
                if (documentVersion != access.DocumentVersion)
                    throw new CaptureFailedException("Die Seite hat während der Aufnahme das Dokument gewechselt.");

                // Freeze the browser before writing; later navigation cannot change
                // the validated HTML, screenshot or the drained resource collection.
                await ClosePageAsync(page);
                var prefix = $"snapshots/{job.Id:N}";
                var manifest = await StoreResourcesAsync(prefix, normalizedUrl, html, screenshot, collector.Resources,
                    cancellationToken, collector.Incomplete);
                var duration = (long)(DateTime.UtcNow - started).TotalMilliseconds;

                return new CaptureResult(
                    manifest.Incomplete ? SnapshotQuality.Incomplete : SnapshotQuality.Complete,
                    prefix,
                    manifest.StorageBytes, manifest.ResourceCount, duration, manifest.StoredObjectKeys);
            }
            catch (Exception exception) when (exception is CaptureChallengeEncounteredException ||
                (exception is PlaywrightException && access.ChallengeObserved))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (attempt == browserOptions.ChallengeReloadAttempts)
                    throw new CaptureAccessBlockedException();
                await access.WaitForAccessAsync(cancellationToken);
                logger.LogInformation("Job {JobId}: wiederhole saubere Aufnahme nach Challenge ({Attempt}/{Limit})",
                    job.Id, attempt + 1, browserOptions.ChallengeReloadAttempts);
            }
            finally
            {
                await ClosePageAsync(page);
                try
                {
                    await collector.DisposeAsync();
                }
                catch (Exception exception)
                {
                    // StopAsync above propagates collection failures on the success
                    // path. Cleanup must preserve the original capture/access error.
                    logger.LogDebug(exception, "Ressourcensammler für Job {JobId} beendet", job.Id);
                }
            }
        }

        throw new CaptureAccessBlockedException();
    }

    private async Task ClosePageAsync(IPage page)
    {
        try { await page.CloseAsync(); }
        catch (PlaywrightException exception) { logger.LogDebug(exception, "Browserseite bereits beendet"); }
    }

    private async Task CloseBrowserAsync(IBrowser browser)
    {
        try { await browser.CloseAsync(); }
        catch (PlaywrightException exception) { logger.LogDebug(exception, "Browser bereits beendet"); }
    }

    private async Task<CaptureResult> CaptureHtmlAsync(ArchiveJob job, IPage page,
        BrowserRequestRouter requestRouter, DateTime started, IProgress<ProgressUpdate>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(job.UploadedObjectKey))
            throw new CaptureFailedException("Die hochgeladene HTML-Datei fehlt.");

        progress?.Report(new ProgressUpdate(ProgressStep.SourceCheck, 10, "HTML-Datei wird geprüft"));
        var source = await objectStore.GetAsync(job.UploadedObjectKey, cancellationToken) ?? throw new CaptureFailedException("Die hochgeladene HTML-Datei konnte nicht gelesen werden.");
        using var reader = new StreamReader(source.Content);
        var html = await reader.ReadToEndAsync(cancellationToken);
        progress?.Report(new ProgressUpdate(ProgressStep.LoadingPage, 35, "HTML-Datei wird geladen"));
        // Uploaded markup is rendered as an isolated document. It must not turn
        // the worker into a network client for URLs embedded by the uploader.
        using var routeRegistration = requestRouter.Register(page, route => route.AbortAsync());
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        progress?.Report(new ProgressUpdate(ProgressStep.LoadingDynamicContent, 55, "HTML-Datei wird vorbereitet"));
        var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions { FullPage = false, Type = ScreenshotType.Png });
        var prefix = $"snapshots/{job.Id:N}";
        var manifest = await StoreResourcesAsync(prefix, null, html, screenshot, new Dictionary<string, CapturedResource>(), cancellationToken);
        var duration = (long)(DateTime.UtcNow - started).TotalMilliseconds;
        try
        {
            await objectStore.DeleteAsync(job.UploadedObjectKey, cancellationToken);
        }
        catch
        {
            await DeleteObjectsAsync(manifest.StoredObjectKeys);
            throw;
        }

        return new CaptureResult(SnapshotQuality.Complete, prefix, manifest.StorageBytes, 0, duration,
            manifest.StoredObjectKeys);
    }

    private async Task LoadDynamicContentAsync(IPage page, CancellationToken cancellationToken)
    {
        await page.EvaluateAsync(@"() => {
            window.__archiveMutationCount = 0;
            window.__archiveMutationObserver?.disconnect();
            window.__archiveMutationObserver = new MutationObserver(() => window.__archiveMutationCount++);
            window.__archiveMutationObserver.observe(document.documentElement, { childList: true, subtree: true, attributes: true });
        }");

        for (var round = 0; round < _limits.MaxScrollRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await page.EvaluateAsync(@"() => {
                window.scrollBy(0, Math.max(window.innerHeight * 0.9, 500));
            }");
            await Task.Delay(_limits.QuietPeriod, cancellationToken);
            var dimensions = await page.EvaluateAsync<ScrollDimensions>(@"() => ({
                top: window.scrollY,
                height: Math.max(document.body.scrollHeight, document.documentElement.scrollHeight),
                viewport: window.innerHeight,
                mutations: window.__archiveMutationCount || 0
            })");
            if (dimensions.Top + dimensions.Viewport >= dimensions.Height - 4)
            {
                await Task.Delay(_limits.QuietPeriod, cancellationToken);
                var finalHeight = await page.EvaluateAsync<int>("() => Math.max(document.body.scrollHeight, document.documentElement.scrollHeight)");
                var finalMutations = await page.EvaluateAsync<int>("() => window.__archiveMutationCount || 0");
                if (finalHeight <= dimensions.Height && finalMutations <= dimensions.Mutations)
                    break;
            }
        }

        await page.EvaluateAsync(@"() => {
            document.querySelectorAll('img[loading=""lazy""], iframe[data-src], [data-lazy-src], [data-src]').forEach((element) => {
                element.scrollIntoView({ block: 'center', behavior: 'instant' });
            });
            window.__archiveMutationObserver?.disconnect();
            window.scrollTo(0, 0);
        }");
        await Task.Delay(_limits.QuietPeriod, cancellationToken);
    }

    private async Task<StoredManifest> StoreResourcesAsync(string prefix, string? originalUrl, string html, byte[] screenshot,
        IReadOnlyDictionary<string, CapturedResource> captured, CancellationToken cancellationToken, bool incomplete = false)
    {
        var map = captured.Values.ToDictionary(x => x.Url, x => $"/api/snapshots/{prefix.Split('/').Last()}/content/assets/{Hash(x.Url)}", StringComparer.Ordinal);
        var rewrittenHtml = RewriteReferences(html, map, originalUrl, $"/api/snapshots/{prefix.Split('/').Last()}/content/");
        var htmlKey = $"{prefix}/index.html";
        var screenshotKey = $"{prefix}/screenshot.png";
        var manifestKey = $"{prefix}/manifest.json";
        var totalBytes = 0L;
        var storedKeys = new List<string>(captured.Count + 3);

        try
        {
            var rewrittenHtmlBytes = Encoding.UTF8.GetBytes(rewrittenHtml);
            totalBytes += rewrittenHtmlBytes.LongLength;
            EnsureStorageLimit(totalBytes);
            storedKeys.Add(htmlKey);
            await PutBytesAsync(htmlKey, rewrittenHtmlBytes, "text/html; charset=utf-8", cancellationToken);
            totalBytes += screenshot.LongLength;
            EnsureStorageLimit(totalBytes);
            storedKeys.Add(screenshotKey);
            await PutBytesAsync(screenshotKey, screenshot, "image/png", cancellationToken);

            foreach (var resource in captured.Values)
            {
                var key = $"{prefix}/assets/{Hash(resource.Url)}";
                EnsureStorageLimit(totalBytes + resource.Body.LongLength);
                var body = resource.ContentType.Equals("text/css", StringComparison.OrdinalIgnoreCase)
                    ? Encoding.UTF8.GetBytes(RewriteReferences(Encoding.UTF8.GetString(resource.Body), map, resource.Url, $"/api/snapshots/{prefix.Split('/').Last()}/content/"))
                    : resource.Body;
                EnsureStorageLimit(totalBytes + body.LongLength);
                storedKeys.Add(key);
                await PutBytesAsync(key, body, resource.ContentType, cancellationToken);
                totalBytes += body.LongLength;
            }

            var manifest = new
            {
                version = 1,
                htmlObjectKey = htmlKey,
                screenshotObjectKey = screenshotKey,
                resources = captured.Values.Select(x => new { url = x.Url, contentType = x.ContentType, objectKey = $"{prefix}/assets/{Hash(x.Url)}" }),
                storageBytes = totalBytes,
                incomplete
            };
            var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, new JsonSerializerOptions { WriteIndented = true });
            EnsureStorageLimit(totalBytes + manifestBytes.LongLength);
            storedKeys.Add(manifestKey);
            await PutBytesAsync(manifestKey, manifestBytes, "application/json", cancellationToken);
            totalBytes += manifestBytes.LongLength;

            return new StoredManifest(totalBytes, captured.Count, incomplete, storedKeys.ToArray());
        }
        catch
        {
            await DeleteObjectsAsync(storedKeys);
            throw;
        }
    }

    private async Task DeleteObjectsAsync(IEnumerable<string> keys)
    {
        foreach (var key in keys.Distinct(StringComparer.Ordinal).Reverse())
        {
            try { await objectStore.DeleteAsync(key, CancellationToken.None); }
            catch (Exception exception) { logger.LogWarning(exception, "Unvollständiges Archivobjekt {ObjectKey} konnte nicht gelöscht werden", key); }
        }
    }

    private void EnsureStorageLimit(long totalBytes)
    {
        if (totalBytes > _limits.MaxStorageBytes)
            throw new CaptureDiscardedException("Das Speicherlimit wurde überschritten.");
    }

    private async Task PutBytesAsync(string key, byte[] bytes, string contentType, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(bytes, writable: false);
        await objectStore.PutAsync(key, stream, contentType, cancellationToken);
    }

    private static string RewriteReferences(string html, IReadOnlyDictionary<string, string> map, string? originalUrl, string contentBase)
    {
        var rewritten = html;
        foreach (var item in map.OrderByDescending(x => x.Key.Length))
        {
            rewritten = rewritten.Replace(item.Key, item.Value, StringComparison.Ordinal);
            rewritten = rewritten.Replace(item.Key.Replace("&", "&amp;", StringComparison.Ordinal), item.Value, StringComparison.Ordinal);
        }

        if (!string.IsNullOrWhiteSpace(originalUrl) && Uri.TryCreate(originalUrl, UriKind.Absolute, out var baseUri))
        {
            rewritten = Regex.Replace(rewritten, "(?<prefix>\\b(?:src|href|poster)\\s*=\\s*)(?<quote>[\\\"'])(?<value>.*?)(?:\\k<quote>)", match =>
            {
                var value = match.Groups["value"].Value;
                if (!Uri.TryCreate(baseUri, value, out var absolute) || !map.TryGetValue(absolute.ToString(), out var replacement))
                    return match.Value;
                return $"{match.Groups["prefix"].Value}{match.Groups["quote"].Value}{replacement}{match.Groups["quote"].Value}";
            }, RegexOptions.IgnoreCase);

            rewritten = Regex.Replace(rewritten, "url\\(\\s*[\\\"'](?<value>[^\\\"']+)[\\\"']\\s*\\)", match =>
            {
                if (!Uri.TryCreate(baseUri, match.Groups["value"].Value, out var absolute) || !map.TryGetValue(absolute.ToString(), out var replacement))
                    return match.Value;
                return $"url('{replacement}')";
            }, RegexOptions.IgnoreCase);

            rewritten = Regex.Replace(rewritten, @"url\(\s*(?<value>[^)\s]+)\s*\)", match =>
            {
                if (!Uri.TryCreate(baseUri, match.Groups["value"].Value, out var absolute) || !map.TryGetValue(absolute.ToString(), out var replacement))
                    return match.Value;
                return $"url('{replacement}')";
            }, RegexOptions.IgnoreCase);
        }

        var localBase = $"<base href=\"{contentBase}\">";
        rewritten = Regex.Replace(rewritten, "<base\\b[^>]*>", localBase, RegexOptions.IgnoreCase);
        return rewritten.Contains("<base ", StringComparison.OrdinalIgnoreCase)
            ? rewritten
            : rewritten.Replace("<head>", $"<head>{localBase}", StringComparison.OrdinalIgnoreCase);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class ScrollDimensions
    {
        public double Top { get; set; }
        public int Height { get; set; }
        public int Viewport { get; set; }
        public int Mutations { get; set; }
    }
    private sealed record StoredManifest(long StorageBytes, int ResourceCount, bool Incomplete,
        IReadOnlyList<string> StoredObjectKeys);
}
