using Archive.Core.Processing;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Archive.Core.Entities;
using Archive.Core.Jobs;
using Archive.Core.Options;
using Archive.Core.Storage;
using Archive.Worker.Capture.Browser.Access;
using Archive.Worker.Capture.Crawling;
using Archive.Worker.Capture.Network;
using Archive.Worker.Storage;
using Archive.Worker.Capture.Replay;
using Microsoft.Playwright;
using Archive.Core.Entities.Processing;

namespace Archive.Worker.Capture.Browser;

public sealed class BrowserCaptureEngine(IArchiveObjectStore objectStore, ArchiveOptions options,
    ArchiveBrowserOptions browserOptions, WebBotAuthSigner webBotAuthSigner, CrawlPolicyService crawlPolicy,
    ILogger<BrowserCaptureEngine> logger)
{
    private readonly CaptureLimits _limits = options.ToCaptureLimits();

    public async Task<CaptureResult> CaptureAsync(CaptureInput job, IProgress<ProgressStep>? progress, CancellationToken cancellationToken)
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

    private async Task<CaptureResult> CaptureUrlAsync(CaptureInput job, IBrowserContext context,
        BrowserRequestRouter requestRouter, DateTime started,
        IProgress<ProgressStep>? progress, CancellationToken cancellationToken)
    {
        if (!UrlNormalizer.TryNormalizeSource(job.OriginUrl, out var normalizedUrl, out var urlError))
            throw new CaptureFailedException(urlError);

        progress?.Report(ProgressStep.SourceCheck);
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
                progress?.Report(ProgressStep.LoadingPage);
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
                progress?.Report(ProgressStep.LoadingDynamicContent);

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
                progress?.Report(ProgressStep.SavingSnapshot);
                var manifest = await StoreResourcesAsync(job.Id, normalizedUrl, html, screenshot, collector.Resources,
                    collector.MainDocument, cancellationToken, collector.FailedUrls);
                var duration = (long)(DateTime.UtcNow - started).TotalMilliseconds;

                return new CaptureResult(
                    manifest.Incomplete ? SnapshotQuality.Incomplete : SnapshotQuality.Complete,
                    manifest.ContentPrefix,
                    manifest.StorageBytes, manifest.ResourceCount, duration, manifest.ReplayVersion,
                    manifest.StoredObjectKeys);
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

    private async Task<StoredManifest> StoreResourcesAsync(Guid snapshotId, string? originalUrl, string fallbackHtml,
        byte[] screenshot, IReadOnlyDictionary<string, CapturedResource> captured, CapturedMainDocument? mainDocument,
        CancellationToken cancellationToken, IReadOnlyList<string> missingUrls)
    {
        var writer = new SnapshotObjectWriter(objectStore, snapshotId, _limits.MaxStorageBytes, logger);
        const string contentBase = "./";
        var map = captured.Values.ToDictionary(x => x.Url, x => $"assets/{Hash(x.Url)}", StringComparer.Ordinal);
        var sourceBytes = mainDocument?.Body ?? Encoding.UTF8.GetBytes(fallbackHtml);
        var sourceUrl = mainDocument?.Url ?? originalUrl;
        var sourceType = mainDocument?.ContentType ?? "text/html; charset=utf-8";
        var sourceHtml = SnapshotTextEncoding.Decode(sourceBytes, sourceType, out _);
        var replayVersion = (int?)SnapshotReplayManifest.CurrentReplayVersion;
        string? replayDiagnostic = null;
        bool containsExecutableContent;
        string rewrittenHtml;
        try
        {
            var rewrittenSource = RewriteReferences(sourceHtml, map, sourceUrl, contentBase, htmlDocument: true);
            var replayDocument = ReplayDocumentNormalizer.Normalize(rewrittenSource, sourceUrl, map, "index.html");
            rewrittenHtml = replayDocument.Html;
            containsExecutableContent = replayDocument.ContainsExecutableContent;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A readable capture remains valuable; only the interactive compatibility claim is withheld.
            replayVersion = null;
            replayDiagnostic = exception.GetType().Name;
            containsExecutableContent = Regex.IsMatch(fallbackHtml, "<script\\b|\\son[a-z]+\\s*=",
                RegexOptions.IgnoreCase);
            rewrittenHtml = RewriteReferences(fallbackHtml, map, originalUrl, contentBase, htmlDocument: true);
            logger.LogWarning(exception, "Replay normalization for snapshot {SnapshotId} failed; stored static fallback",
                snapshotId);
        }
        var htmlKey = writer.GetObjectKey("index.html");
        var screenshotKey = writer.GetObjectKey("screenshot.png");
        var sourceKey = writer.GetObjectKey("source.html");

        try
        {
            var rewrittenHtmlBytes = Encoding.UTF8.GetBytes(rewrittenHtml);
            await writer.WriteAsync("index.html", rewrittenHtmlBytes, "text/html; charset=utf-8", cancellationToken);
            await writer.WriteAsync("screenshot.png", screenshot, "image/png", cancellationToken);
            await writer.WriteAsync("source.html", sourceBytes, sourceType, cancellationToken);

            var manifestResources = new List<SnapshotReplayResource>
            {
                new()
                {
                    Path = "index.html",
                    SourceUrl = sourceUrl,
                    ContentType = "text/html; charset=utf-8",
                    ObjectKey = htmlKey,
                    Sha256 = Hash(rewrittenHtmlBytes),
                    Role = SnapshotReplayResourceRoles.RootDocument
                }
            };
            foreach (var resource in captured.Values)
            {
                writer.EnsureCapacity(resource.Body.LongLength);
                var assetPath = $"assets/{Hash(resource.Url)}";
                var isCss = resource.ContentType.StartsWith("text/css", StringComparison.OrdinalIgnoreCase);
                var isHtml = resource.ContentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) ||
                    resource.ContentType.StartsWith("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);
                var isJavaScript = resource.ContentType.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
                    resource.ContentType.Contains("ecmascript", StringComparison.OrdinalIgnoreCase);
                var body = resource.Body;
                var contentType = resource.ContentType;
                if (isCss || isHtml)
                {
                    var relativeMap = isHtml
                        ? map
                        : map.ToDictionary(pair => pair.Key,
                            pair => SnapshotResourcePath.RelativeTo(assetPath, pair.Value), StringComparer.Ordinal);
                    var text = SnapshotTextEncoding.Decode(resource.Body, resource.ContentType, out _);
                    var resourceBase = isHtml ? SnapshotResourcePath.HtmlBaseFor(assetPath) : contentBase;
                    var rewritten = RewriteReferences(text, relativeMap, resource.Url, resourceBase,
                        htmlDocument: isHtml);
                    if (isHtml && replayVersion is not null)
                    {
                        try
                        {
                            rewritten = ReplayDocumentNormalizer.Normalize(rewritten, resource.Url, relativeMap,
                                assetPath).Html;
                        }
                        catch (Exception exception) when (exception is not OperationCanceledException)
                        {
                            // Nested-document incompatibility downgrades the whole snapshot to static playback.
                            replayVersion = null;
                            replayDiagnostic ??= exception.GetType().Name;
                            logger.LogWarning(exception,
                                "Nested replay normalization for snapshot {SnapshotId} failed; stored static fallback",
                                snapshotId);
                        }
                    }
                    body = Encoding.UTF8.GetBytes(rewritten);
                    contentType = isHtml ? "text/html; charset=utf-8" : "text/css; charset=utf-8";
                }
                else if (isJavaScript && replayVersion is not null)
                {
                    var relativeMap = map.ToDictionary(pair => pair.Key,
                        pair => SnapshotResourcePath.RelativeTo(assetPath, pair.Value), StringComparer.Ordinal);
                    var text = SnapshotTextEncoding.Decode(resource.Body, resource.ContentType, out _);
                    body = Encoding.UTF8.GetBytes(JavaScriptReferenceRewriter.Rewrite(text, resource.Url, relativeMap));
                    contentType = "text/javascript; charset=utf-8";
                    containsExecutableContent = true;
                }
                await writer.WriteAsync(assetPath, body, contentType, cancellationToken);
                manifestResources.Add(new()
                {
                    Path = assetPath,
                    SourceUrl = resource.Url,
                    ContentType = contentType,
                    ObjectKey = writer.GetObjectKey(assetPath),
                    Sha256 = Hash(body),
                    Role = isHtml ? SnapshotReplayResourceRoles.NestedDocument : SnapshotReplayResourceRoles.Asset
                });
            }

            var manifest = new SnapshotReplayManifest
            {
                Version = SnapshotReplayManifest.CurrentManifestVersion,
                Provenance = new()
                {
                    DerivedDisplayDocument = true,
                    SourceObjectKey = sourceKey,
                    SourceSha256 = Hash(sourceBytes),
                    IndexSha256 = Hash(rewrittenHtmlBytes),
                    SourceUrl = sourceUrl,
                    SourceMimeType = sourceType,
                    SourceStatusCode = mainDocument?.StatusCode,
                    CapturedAt = DateTimeOffset.UtcNow,
                    DocumentState = mainDocument is null ? "post-execution-fallback" : "original-response",
                    RedirectChain = mainDocument?.RedirectChain ?? []
                },
                Replay = new()
                {
                    Version = replayVersion,
                    Normalizer = "archive-replay-v1",
                    ContainsExecutableContent = containsExecutableContent,
                    Diagnostic = replayDiagnostic
                },
                Resources = manifestResources,
                MissingUrls = missingUrls.Take(100).ToArray(),
                MissingUrlCount = missingUrls.Count,
                Incomplete = missingUrls.Count > 0
            };
            await writer.WriteManifestAsync(manifest, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);

            return new StoredManifest(writer.ContentPrefix, writer.StorageBytes, captured.Count, missingUrls.Count > 0,
                replayVersion, writer.StoredObjectKeys);
        }
        catch
        {
            await writer.RollbackAsync();
            throw;
        }
    }

    private static string RewriteReferences(string html, IReadOnlyDictionary<string, string> map, string? originalUrl,
        string contentBase, bool htmlDocument)
    {
        var rewritten = html;
        var resolutionUrl = htmlDocument
            ? SnapshotResourcePath.ResolveDocumentBase(html, originalUrl)
            : originalUrl;
        foreach (var item in map.OrderByDescending(x => x.Key.Length))
        {
            rewritten = rewritten.Replace(item.Key, item.Value, StringComparison.Ordinal);
            rewritten = rewritten.Replace(item.Key.Replace("&", "&amp;", StringComparison.Ordinal), item.Value, StringComparison.Ordinal);
        }

        if (!string.IsNullOrWhiteSpace(resolutionUrl) && Uri.TryCreate(resolutionUrl, UriKind.Absolute, out var baseUri))
        {
            if (htmlDocument)
            {
                rewritten = Regex.Replace(rewritten, "(?<prefix>\\b(?:src|href|poster|data-[\\w-]+|srcset)\\s*=\\s*)(?<quote>[\\\"'])(?<value>.*?)(?:\\k<quote>)", match =>
                {
                    var value = match.Groups["value"].Value;
                    if (match.Groups["prefix"].Value.Contains("srcset", StringComparison.OrdinalIgnoreCase))
                    {
                        value = SnapshotSourceSet.Rewrite(value, candidate =>
                        {
                            var decoded = WebUtility.HtmlDecode(candidate);
                            if (map.Values.Contains(decoded, StringComparer.Ordinal))
                                return decoded;
                            return TryResolveCapturedUrl(baseUri, decoded, map, out var candidateReplacement)
                                ? candidateReplacement : null;
                        });
                        return $"{match.Groups["prefix"].Value}{match.Groups["quote"].Value}{value}{match.Groups["quote"].Value}";
                    }
                    if (!TryResolveCapturedUrl(baseUri, value, map, out var replacement))
                        return match.Value;
                    return $"{match.Groups["prefix"].Value}{match.Groups["quote"].Value}{replacement}{match.Groups["quote"].Value}";
                }, RegexOptions.IgnoreCase);
            }

            rewritten = Regex.Replace(rewritten,
                "(?<prefix>@import\\s*)(?<quote>[\\\"'])(?<value>[^\\\"']+)(?:\\k<quote>)", match =>
                {
                    if (!TryResolveCapturedUrl(baseUri, match.Groups["value"].Value, map, out var replacement))
                        return match.Value;
                    return $"{match.Groups["prefix"].Value}{match.Groups["quote"].Value}{replacement}{match.Groups["quote"].Value}";
                }, RegexOptions.IgnoreCase);

            rewritten = Regex.Replace(rewritten, "url\\(\\s*[\\\"'](?<value>[^\\\"']+)[\\\"']\\s*\\)", match =>
            {
                if (!TryResolveCapturedUrl(baseUri, match.Groups["value"].Value, map, out var replacement))
                    return match.Value;
                return $"url('{replacement}')";
            }, RegexOptions.IgnoreCase);

            rewritten = Regex.Replace(rewritten, @"url\(\s*(?<value>[^)\s]+)\s*\)", match =>
            {
                if (!TryResolveCapturedUrl(baseUri, match.Groups["value"].Value, map, out var replacement))
                    return match.Value;
                return $"url('{replacement}')";
            }, RegexOptions.IgnoreCase);
        }

        if (!htmlDocument)
            return rewritten;

        var localBase = $"<base href=\"{contentBase}\">";
        rewritten = Regex.Replace(rewritten, "<base\\b[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        if (Regex.IsMatch(rewritten, "<head\\b", RegexOptions.IgnoreCase))
            return Regex.Replace(rewritten, "<head\\b[^>]*>", match => match.Value + localBase,
                RegexOptions.IgnoreCase);
        if (Regex.IsMatch(rewritten, "<html\\b", RegexOptions.IgnoreCase))
            return Regex.Replace(rewritten, "<html\\b[^>]*>", match => match.Value + "<head>" + localBase + "</head>",
                RegexOptions.IgnoreCase);
        return "<head>" + localBase + "</head>" + rewritten;
    }

    private static bool TryResolveCapturedUrl(Uri baseUri, string rawValue,
        IReadOnlyDictionary<string, string> map, out string replacement)
    {
        var value = WebUtility.HtmlDecode(rawValue);
        if (!Uri.TryCreate(baseUri, value, out var absolute))
        {
            replacement = string.Empty;
            return false;
        }

        if (!string.IsNullOrEmpty(absolute.Fragment))
            absolute = new UriBuilder(absolute) { Fragment = string.Empty }.Uri;
        return map.TryGetValue(absolute.ToString(), out replacement!);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private sealed class ScrollDimensions
    {
        public double Top { get; set; }
        public int Height { get; set; }
        public int Viewport { get; set; }
        public int Mutations { get; set; }
    }
    private sealed record StoredManifest(string ContentPrefix, long StorageBytes, int ResourceCount, bool Incomplete,
        int? ReplayVersion,
        IReadOnlyList<string> StoredObjectKeys);
}
