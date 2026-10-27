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
using Archive.Worker.Capture.Browser.Crawl;
using Archive.Worker.Capture.Network;
using Archive.Worker.Storage;
using Microsoft.Playwright;
using Archive.Core.Entities.Processing;

namespace Archive.Worker.Capture.Browser;

public sealed class BrowserCaptureEngine(IArchiveObjectStore objectStore, ArchiveOptions options,
    ArchiveBrowserOptions browserOptions, ICaptureNetworkGuard networkGuard,
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
                // The explicit Chromium channel uses the modern container path on Linux,
                // but can fail to spawn the bundled browser on Windows development hosts.
                Channel = browserOptions.Headless && !OperatingSystem.IsWindows() ? "chromium" : null,
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
            await using var requestRouter = new BrowserRequestRouter(context);
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

        var resourceArchive = new CapturedResourceArchive(_limits);
        var root = await CaptureDocumentAsync(job.Id, normalizedUrl, context, requestRouter, networkGuard,
            resourceArchive, requiredOrigin: null, captureScreenshot: true, progress, cancellationToken);
        if (!Uri.TryCreate(root.Url, UriKind.Absolute, out var effectiveOrigin))
            throw new CaptureFailedException("The source page's final URL is invalid.");

        root.AddAliases([normalizedUrl, root.Url, .. root.RedirectChain]);
        resourceArchive.RemovePromotedDocument(root.Aliases);
        var documents = new List<CapturedDocument> { root };
        var documentsByUrl = new Dictionary<string, CapturedDocument>(StringComparer.Ordinal);
        var parents = new Dictionary<CapturedDocument, CapturedDocument?>(ReferenceEqualityComparer.Instance)
        {
            [root] = null
        };
        foreach (var alias in root.Aliases)
            documentsByUrl[alias] = root;

        var pending = new Queue<PendingRoute>();
        var scheduled = new HashSet<string>(root.Aliases, StringComparer.Ordinal);
        var missingRoutes = new HashSet<string>(StringComparer.Ordinal);
        var scheduledLinkedPages = 0;

        EnqueueDiscoveredLinks(root);
        while (pending.TryDequeue(out var pendingRoute))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var routeUrl = pendingRoute.Url;
            if (documentsByUrl.ContainsKey(routeUrl))
                continue;

            CapturedDocument document;
            try
            {
                document = await CaptureDocumentAsync(job.Id, routeUrl, context, requestRouter, networkGuard,
                    resourceArchive, effectiveOrigin, captureScreenshot: false, progress, cancellationToken);
            }
            catch (CaptureFailedException exception)
            {
                missingRoutes.Add(routeUrl);
                logger.LogWarning(exception, "Job {JobId}: verlinkte Route {RouteUrl} konnte nicht archiviert werden",
                    job.Id, routeUrl);
                continue;
            }

            document.AddAliases([routeUrl, document.Url, .. document.RedirectChain]);
            var existing = document.Aliases.Select(alias => documentsByUrl.GetValueOrDefault(alias))
                .FirstOrDefault(candidate => candidate is not null);
            if (existing is not null)
            {
                existing.AddAliases(document.Aliases);
                foreach (var alias in document.Aliases)
                    documentsByUrl[alias] = existing;
                resourceArchive.RemovePromotedDocument(document.Aliases);
                resourceArchive.ReleaseDocument(document.ReservedBytes);
                continue;
            }

            documents.Add(document);
            parents[document] = pendingRoute.Parent;
            foreach (var alias in document.Aliases)
                documentsByUrl[alias] = document;
            resourceArchive.RemovePromotedDocument(document.Aliases);
            EnqueueDiscoveredLinks(document);
        }

        progress?.Report(ProgressStep.SavingSnapshot);
        var missingUrls = resourceArchive.SnapshotFailedUrls().Concat(missingRoutes)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var manifest = await StoreResourcesAsync(job.Id, documents, parents, root.Screenshot!,
            resourceArchive.SnapshotResources(), cancellationToken, missingUrls);
        var duration = (long)(DateTime.UtcNow - started).TotalMilliseconds;

        return new CaptureResult(
            manifest.Incomplete ? SnapshotQuality.Incomplete : SnapshotQuality.Complete,
            manifest.ContentPrefix,
            manifest.StorageBytes, manifest.ResourceCount, duration, manifest.StoredObjectKeys);

        void EnqueueDiscoveredLinks(CapturedDocument document)
        {
            foreach (var href in document.DiscoveredLinks)
            {
                if (!SnapshotCrawlUrl.TryNormalizeInScope(href, effectiveOrigin, out var candidate) ||
                    documentsByUrl.ContainsKey(candidate) || !scheduled.Add(candidate))
                    continue;
                if (browserOptions.MaxLinkedPages == 0)
                    continue;
                if (scheduledLinkedPages >= browserOptions.MaxLinkedPages)
                {
                    missingRoutes.Add(candidate);
                    continue;
                }

                scheduledLinkedPages++;
                pending.Enqueue(new(candidate, document));
            }
        }
    }

    // A fresh page isolates late responses from earlier documents while the shared
    // context retains challenge cookies and the resource archive de-duplicates bytes.
    private async Task<CapturedDocument> CaptureDocumentAsync(Guid jobId, string sourceUrl, IBrowserContext context,
        BrowserRequestRouter requestRouter, ICaptureNetworkGuard networkGuard,
        CapturedResourceArchive resourceArchive, Uri? requiredOrigin, bool captureScreenshot,
        IProgress<ProgressStep>? progress, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= browserOptions.ChallengeReloadAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = await context.NewPageAsync();
            using var access = new CapturePageAccess(page, sourceUrl, browserOptions, networkGuard,
                requestRouter, logger, requiredOrigin);
            var collector = new CapturedResourceCollector(page, resourceArchive, logger, cancellationToken);
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
                var discoveredLinks = await DiscoverLinksAsync(page, cancellationToken);
                var html = await CloudflareChallengeDetector.SnapshotHtmlAsync(page, cancellationToken);
                var screenshot = captureScreenshot
                    ? await page.ScreenshotAsync(new PageScreenshotOptions
                        { FullPage = false, Type = ScreenshotType.Png }).WaitAsync(cancellationToken)
                    : null;
                await collector.StopAsync();
                await access.VerifyCaptureAsync(cancellationToken);
                if (documentVersion != access.DocumentVersion)
                    throw new CaptureFailedException("Die Seite hat während der Aufnahme das Dokument gewechselt.");

                var finalUrl = access.FinalUrl;
                if (finalUrl is null || !UrlNormalizer.TryNormalizeSource(finalUrl, out finalUrl, out _))
                    throw new CaptureFailedException("The document's final URL could not be determined.");
                var title = NormalizePageTitle(await page.TitleAsync().WaitAsync(cancellationToken));
                var mainDocument = collector.CommitMainDocument();
                if (requiredOrigin is not null && mainDocument is null)
                    throw new CaptureFailedException("The linked route did not return a supported HTML document.");
                await ClosePageAsync(page);
                return new CapturedDocument(
                    sourceUrl,
                    finalUrl,
                    title,
                    mainDocument?.ContentType ?? "text/html; charset=utf-8",
                    mainDocument?.StatusCode,
                    mainDocument?.Body ?? Encoding.UTF8.GetBytes(html),
                    html,
                    mainDocument?.RedirectChain ?? [],
                    discoveredLinks,
                    screenshot,
                    mainDocument?.Body.LongLength ?? 0);
            }
            catch (Exception exception) when (exception is CaptureChallengeEncounteredException ||
                (exception is PlaywrightException && access.ChallengeObserved))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (attempt == browserOptions.ChallengeReloadAttempts)
                    throw new CaptureAccessBlockedException();
                await access.WaitForAccessAsync(cancellationToken);
                logger.LogInformation("Job {JobId}: wiederhole saubere Aufnahme nach Challenge ({Attempt}/{Limit})",
                    jobId, attempt + 1, browserOptions.ChallengeReloadAttempts);
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
                    logger.LogDebug(exception, "Ressourcensammler für Job {JobId} beendet", jobId);
                }
            }
        }

        throw new CaptureAccessBlockedException();
    }

    private static Task<string[]> DiscoverLinksAsync(IPage page, CancellationToken cancellationToken) =>
        page.EvaluateAsync<string[]>(@"() => Array.from(document.querySelectorAll('a[href], area[href]'))
            .filter(element => !element.hasAttribute('download'))
            .map(element => element.href)
            .filter(Boolean)").WaitAsync(cancellationToken);

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

    private async Task<StoredManifest> StoreResourcesAsync(Guid snapshotId,
        IReadOnlyList<CapturedDocument> documents,
        IReadOnlyDictionary<CapturedDocument, CapturedDocument?> parents, byte[] screenshot,
        IReadOnlyDictionary<string, CapturedResource> captured, CancellationToken cancellationToken,
        IReadOnlyList<string> missingUrls)
    {
        var writer = new SnapshotObjectWriter(objectStore, snapshotId, _limits.MaxStorageBytes, logger);
        var root = documents[0];
        var documentPaths = new Dictionary<CapturedDocument, string>(ReferenceEqualityComparer.Instance);
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < documents.Count; index++)
        {
            var document = documents[index];
            var path = index == 0 ? "index.html" : $"pages/{Hash(document.Url)}.html";
            documentPaths[document] = path;
            foreach (var alias in document.Aliases)
                map.TryAdd(alias, path);
        }

        var pageTree = new SnapshotPageTree
        {
            RootPath = documentPaths[root],
            Pages = documents.Select(document => new SnapshotPageTreeEntry
            {
                Path = documentPaths[document],
                ParentPath = parents[document] is { } parent ? documentPaths[parent] : null,
                SourceUrl = document.Url,
                Title = document.Title
            }).ToArray()
        };

        foreach (var resource in captured.Values)
        {
            var url = AbsoluteUrlKey(resource.Url);
            if (url is not null)
                map.TryAdd(url, $"assets/{Hash(url)}");
        }

        var sourceKey = writer.GetObjectKey("source.html");
        var manifestResources = new List<SnapshotResource>();
        string? interactionDiagnostic = null;
        var containsExecutableContent = false;
        byte[]? rootDocumentBytes = null;

        try
        {
            await writer.WriteAsync("screenshot.png", screenshot, "image/png", cancellationToken);
            await writer.WriteAsync("source.html", root.SourceBody, root.ContentType, cancellationToken);

            foreach (var document in documents)
            {
                var documentPath = documentPaths[document];
                var sourceHtml = SnapshotTextEncoding.Decode(document.SourceBody, document.ContentType, out _);
                string rewrittenHtml;
                try
                {
                    var rewrittenSource = RewriteReferences(sourceHtml, map, document.Url,
                        SnapshotResourcePath.HtmlBaseFor(documentPath), htmlDocument: true);
                    var normalized = SnapshotDocumentNormalizer.Normalize(rewrittenSource, document.Url, map,
                        documentPath);
                    rewrittenHtml = normalized.Html;
                    containsExecutableContent |= normalized.ContainsExecutableContent;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    interactionDiagnostic ??= exception.GetType().Name;
                    containsExecutableContent |= Regex.IsMatch(document.FallbackHtml,
                        "<script\\b|\\son[a-z]+\\s*=", RegexOptions.IgnoreCase);
                    rewrittenHtml = RewriteReferences(document.FallbackHtml, map, document.Url,
                        SnapshotResourcePath.HtmlBaseFor(documentPath), htmlDocument: true);
                    logger.LogWarning(exception,
                        "Snapshot document normalization for {SnapshotId} and {DocumentUrl} failed; stored rewritten fallback",
                        snapshotId, document.Url);
                }

                var body = Encoding.UTF8.GetBytes(rewrittenHtml);
                await writer.WriteAsync(documentPath, body, "text/html; charset=utf-8", cancellationToken);
                if (ReferenceEquals(document, root))
                    rootDocumentBytes = body;
                manifestResources.Add(new()
                {
                    Path = documentPath,
                    SourceUrl = document.Url,
                    ContentType = "text/html; charset=utf-8",
                    ObjectKey = writer.GetObjectKey(documentPath),
                    Sha256 = Hash(body),
                    Role = ReferenceEquals(document, root)
                        ? SnapshotResourceRoles.RootDocument
                        : SnapshotResourceRoles.NestedDocument
                });
            }

            foreach (var resource in captured.Values)
            {
                var resourceUrl = AbsoluteUrlKey(resource.Url);
                if (resourceUrl is null || documentPaths.Keys.Any(document => document.Aliases.Contains(resourceUrl)))
                    continue;
                var assetPath = map[resourceUrl];
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
                    var resourceBase = isHtml ? SnapshotResourcePath.HtmlBaseFor(assetPath) : "./";
                    var rewritten = RewriteReferences(text, relativeMap, resource.Url, resourceBase,
                        htmlDocument: isHtml);
                    if (isHtml)
                    {
                        try
                        {
                            var normalized = SnapshotDocumentNormalizer.Normalize(rewritten, resource.Url, relativeMap,
                                assetPath);
                            rewritten = normalized.Html;
                            containsExecutableContent |= normalized.ContainsExecutableContent;
                        }
                        catch (Exception exception) when (exception is not OperationCanceledException)
                        {
                            interactionDiagnostic ??= exception.GetType().Name;
                            logger.LogWarning(exception,
                                "Nested snapshot normalization for {SnapshotId} failed; stored rewritten fallback",
                                snapshotId);
                        }
                    }
                    body = Encoding.UTF8.GetBytes(rewritten);
                    contentType = isHtml ? "text/html; charset=utf-8" : "text/css; charset=utf-8";
                }
                else if (isJavaScript)
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
                    Role = isHtml ? SnapshotResourceRoles.NestedDocument : SnapshotResourceRoles.Asset
                });
            }

            if (rootDocumentBytes is null)
                throw new InvalidDataException("The root snapshot document was not written.");
            var manifest = new SnapshotManifest
            {
                Version = SnapshotManifest.CurrentVersion,
                Provenance = new()
                {
                    DerivedDisplayDocument = true,
                    SourceObjectKey = sourceKey,
                    SourceSha256 = Hash(root.SourceBody),
                    IndexSha256 = Hash(rootDocumentBytes),
                    SourceUrl = root.Url,
                    SourceMimeType = root.ContentType,
                    SourceStatusCode = root.StatusCode,
                    CapturedAt = DateTimeOffset.UtcNow,
                    DocumentState = root.StatusCode is null ? "post-execution-fallback" : "original-response",
                    RedirectChain = root.RedirectChain
                },
                Interaction = new()
                {
                    Normalizer = SnapshotDocumentNormalizer.Version,
                    ContainsExecutableContent = containsExecutableContent,
                    Diagnostic = interactionDiagnostic
                },
                Resources = manifestResources,
                PageTree = pageTree,
                MissingUrls = missingUrls.Take(100).ToArray(),
                MissingUrlCount = missingUrls.Count,
                Incomplete = missingUrls.Count > 0
            };
            await writer.WriteManifestAsync(manifest, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);

            return new StoredManifest(writer.ContentPrefix, writer.StorageBytes, manifestResources.Count - 1,
                missingUrls.Count > 0, writer.StoredObjectKeys);
        }
        catch
        {
            await writer.RollbackAsync();
            throw;
        }
    }

    private static string? AbsoluteUrlKey(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? SnapshotResourcePath.ToAbsoluteUrlKey(uri)
            : null;

    private static string RewriteReferences(string html, IReadOnlyDictionary<string, string> map, string? originalUrl,
        string contentBase, bool htmlDocument)
    {
        var rewritten = html;
        var resolutionUrl = htmlDocument
            ? SnapshotResourcePath.ResolveDocumentBase(html, originalUrl)
            : originalUrl;
        if (!string.IsNullOrWhiteSpace(resolutionUrl) && Uri.TryCreate(resolutionUrl, UriKind.Absolute, out var baseUri))
        {
            if (htmlDocument)
            {
                rewritten = Regex.Replace(rewritten, "(?<prefix>\\b(?:src|poster|data-[\\w-]+|srcset)\\s*=\\s*)(?<quote>[\\\"'])(?<value>.*?)(?:\\k<quote>)", match =>
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

        return map.TryGetValue(SnapshotResourcePath.ToAbsoluteUrlKey(absolute), out replacement!);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private static string? NormalizePageTitle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var bounded = TruncateUtf16(value, SnapshotManifestReader.MaxPageTitleLength * 8);
        var normalized = Regex.Replace(bounded, "\\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(1)).Trim();
        normalized = string.Concat(normalized.Where(character => !char.IsControl(character)));
        if (normalized.Length == 0)
            return null;
        return TruncateUtf16(normalized, SnapshotManifestReader.MaxPageTitleLength);
    }

    private static string TruncateUtf16(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;
        var length = maxLength;
        if (char.IsHighSurrogate(value[length - 1]) && char.IsLowSurrogate(value[length]))
            length--;
        return value[..length];
    }

    private sealed class ScrollDimensions
    {
        public double Top { get; set; }
        public int Height { get; set; }
        public int Viewport { get; set; }
        public int Mutations { get; set; }
    }
    private sealed record StoredManifest(string ContentPrefix, long StorageBytes, int ResourceCount, bool Incomplete,
        IReadOnlyList<string> StoredObjectKeys);
    private sealed record PendingRoute(string Url, CapturedDocument Parent);
}
