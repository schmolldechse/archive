using System.Diagnostics;
using Archive.Core.Jobs;
using Microsoft.Playwright;

namespace Archive.Worker.Capture;

// One session per page. Response state from a previous document must never approve
// a new document, including a same-URL reload during a Cloudflare challenge.
internal sealed class CapturePageAccess : IDisposable
{
    private readonly IPage _page;
    private readonly string _sourceUrl;
    private readonly Uri _sourceUri;
    private readonly ArchiveBrowserOptions _options;
    private readonly PublicNetworkGuard _networkGuard;
    private readonly BrowserRequestRouter _requestRouter;
    private readonly ILogger _logger;
    private readonly object _sync = new();
    private IRequest? _request;
    private IResponse? _response;
    private bool _committed;
    private bool _blockedNavigation;
    private string? _blockedNetworkHost;
    private bool _challengeObserved;
    private long _challengeStarted;
    private long _documentVersion;
    private IDisposable? _routeRegistration;

    public CapturePageAccess(IPage page, string sourceUrl, ArchiveBrowserOptions options,
        PublicNetworkGuard networkGuard, BrowserRequestRouter requestRouter, ILogger logger)
    {
        _page = page;
        _sourceUrl = sourceUrl;
        _sourceUri = new Uri(sourceUrl);
        _options = options;
        _networkGuard = networkGuard;
        _requestRouter = requestRouter;
        _logger = logger;
        page.Request += OnRequest;
        page.Response += OnResponse;
        page.FrameNavigated += OnFrameNavigated;
    }

    public bool ChallengeObserved { get { lock (_sync) return _challengeObserved; } }
    public long DocumentVersion { get { lock (_sync) return _documentVersion; } }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        _routeRegistration = _requestRouter.Register(_page, route => HandleRouteAsync(route, cancellationToken));
        return Task.CompletedTask;
    }

    private async Task HandleRouteAsync(IRoute route, CancellationToken cancellationToken)
    {
        var request = route.Request;
        if (!await _networkGuard.IsAllowedAsync(request.Url, cancellationToken))
        {
            BlockNetworkTarget(request.Url);
            await route.AbortAsync();
            return;
        }

        var mainFrame = request.IsNavigationRequest && request.Frame == _page.MainFrame;
        if (request.IsNavigationRequest)
        {
            bool allowed;
            lock (_sync)
            {
                allowed = IsAllowedNavigation(request, mainFrame);
                if (!allowed && mainFrame)
                    _blockedNavigation = true;
            }

            if (!allowed)
            {
                await route.AbortAsync();
                return;
            }
        }

        // ContinueAsync bypasses routing for the remaining HTTP redirect chain.
        // Fetch and fulfill exactly one hop for every request so navigation and
        // asset redirects both become new routed requests before they are sent.
        var response = await _requestRouter.FetchAsync(route);
        if (CloudflareChallengeDetector.IsChallengeHeaders(response.Headers))
            ObserveChallenge();

        if (response.Status is >= 300 and < 400)
        {
            if (!response.Headers.TryGetValue("location", out var location) ||
                !Uri.TryCreate(new Uri(response.Url), location, out var redirectUri))
            {
                lock (_sync) _blockedNavigation = true;
                await route.AbortAsync();
                return;
            }

            if (!await _networkGuard.IsAllowedAsync(redirectUri.AbsoluteUri, cancellationToken))
            {
                BlockNetworkTarget(redirectUri.AbsoluteUri);
                await route.AbortAsync();
                return;
            }

            if (!IsAllowedRedirectTarget(redirectUri.AbsoluteUri, request.IsNavigationRequest, mainFrame))
            {
                lock (_sync) _blockedNavigation = true;
                await route.AbortAsync();
                return;
            }
        }

        await route.FulfillAsync(new RouteFulfillOptions { Response = response });
    }

    public async Task NavigateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _page.GotoAsync(_sourceUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Commit })
                .WaitAsync(cancellationToken);
        }
        catch (PlaywrightException exception)
        {
            ThrowIfNetworkBlocked();
            ThrowIfNavigationBlocked();
            // A challenge can replace the first navigation before it commits.
            if (!ChallengeObserved)
                throw new CaptureFailedException("Die URL konnte nicht geladen werden.", exception);
        }
    }

    public async Task WaitForAccessAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfNavigationBlocked();
            ThrowIfNetworkBlocked();
            if (_page.IsClosed)
                throw new CaptureFailedException("Der Browser wurde vor Abschluss der Aufnahme geschlossen.");

            var inspection = await InspectStableDocumentAsync(cancellationToken);
            if (inspection is not null)
            {
                var (response, document) = inspection.Value;
                var challengedResponse = CloudflareChallengeDetector.IsChallengeResponse(response);
                if (document.IsChallenge)
                    ObserveChallenge();

                if (document.Ready && IsSourceUrl(_page.Url) && IsSourceUrl(response.Url) && !document.IsChallenge)
                {
                    if (!challengedResponse)
                    {
                        if (response.Status is < 200 or >= 300)
                            throw new CaptureFailedException($"Die Quellseite antwortet mit HTTP {response.Status}.");
                        return;
                    }

                    // Some interstitials replace their DOM before the first polling
                    // interval. Permit only a fresh attempt; VerifyCaptureAsync still
                    // refuses this response, so it can never be stored.
                    return;
                }
            }

            lock (_sync)
                if (_challengeObserved && Stopwatch.GetElapsedTime(_challengeStarted) >= _options.ChallengeTimeout)
                    throw new CaptureAccessBlockedException();

            await Task.Delay(250, cancellationToken);
        }
    }

    public async Task VerifyCaptureAsync(CancellationToken cancellationToken)
    {
        await WaitForAccessAsync(cancellationToken);
        if (ChallengeObserved)
            throw new CaptureChallengeEncounteredException();
    }

    public async Task EnsureArchivingAllowedAsync(CancellationToken cancellationToken)
    {
        IResponse? response;
        lock (_sync)
            response = _response;
        if (response is null)
            throw new CaptureFailedException("Die Crawl-Direktiven der Seite konnten nicht geprüft werden.");

        if (response.Headers.Any(header =>
                header.Key.Equals("x-robots-tag", StringComparison.OrdinalIgnoreCase) &&
                ContainsArchiveBlock(header.Value)))
            throw new CaptureFailedException("X-Robots-Tag untersagt die Archivierung dieser Seite.");

        var directives = await _page.EvaluateAsync<string[]>(@"() => Array.from(
            document.querySelectorAll('meta[name]'))
            .filter(element => ['robots', 'voldechsearchivebot'].includes(
                (element.getAttribute('name') || '').toLowerCase()))
            .map(element => element.getAttribute('content') || '')").WaitAsync(cancellationToken);
        if (directives.Any(ContainsArchiveBlock))
            throw new CaptureFailedException("Eine Robots-Metadirektive untersagt die Archivierung dieser Seite.");
    }

    private static bool ContainsArchiveBlock(string value) => value.Split([',', ';', ' ', '\t'],
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(token =>
        token.Equals("noarchive", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("none", StringComparison.OrdinalIgnoreCase));

    private async Task<(IResponse Response, CloudflareChallengeDetector.DocumentState Document)?> InspectStableDocumentAsync(
        CancellationToken cancellationToken)
    {
        IResponse? response;
        long version;
        lock (_sync)
        {
            if (!_committed || _response is null)
                return null;
            response = _response;
            version = _documentVersion;
        }

        CloudflareChallengeDetector.DocumentState document;
        try
        {
            document = await CloudflareChallengeDetector.InspectAsync(_page, cancellationToken);
        }
        catch (PlaywrightException exception) when (
            exception.Message.Contains("Execution context was destroyed", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Cannot find context", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        lock (_sync)
            return _committed && version == _documentVersion && ReferenceEquals(response, _response)
                ? (response, document)
                : null;
    }

    private void OnRequest(object? sender, IRequest request)
    {
        if (!request.IsNavigationRequest || request.Frame != _page.MainFrame)
            return;
        lock (_sync)
        {
            // Playwright route handlers run only for the first request of an HTTP
            // redirect chain. Inspect every navigation event as well, including an
            // intermediate disallowed URL followed by a return to the source URL.
            if (!IsAllowedNavigation(request, mainFrame: true))
                _blockedNavigation = true;
            _request = request;
            _response = null;
            _committed = false;
            _documentVersion++;
        }
    }

    private void OnResponse(object? sender, IResponse response)
    {
        if (!response.Request.IsNavigationRequest || response.Frame != _page.MainFrame)
            return;
        lock (_sync)
        {
            if (!ReferenceEquals(_request, response.Request))
                return;
            _response = response;
        }
        if (CloudflareChallengeDetector.IsChallengeResponse(response))
            ObserveChallenge();
    }

    private void OnFrameNavigated(object? sender, IFrame frame)
    {
        if (frame != _page.MainFrame)
            return;
        lock (_sync)
        {
            if (!IsSourceUrl(frame.Url) && !(_challengeObserved &&
                (IsSameOriginChallenge(frame.Url) || IsSourceChallengeContinuation(frame.Url))))
                _blockedNavigation = true;
            _committed = true;
            _documentVersion++;
        }
    }

    private void ObserveChallenge()
    {
        lock (_sync)
        {
            if (_challengeObserved)
                return;
            _challengeObserved = true;
            _challengeStarted = Stopwatch.GetTimestamp();
        }
        _logger.LogInformation("Cloudflare-Challenge für {Host} erkannt; warte bis zu {Seconds} Sekunden auf Freigabe",
            _sourceUri.Host, _options.ChallengeTimeout.TotalSeconds);
    }

    private void ThrowIfNavigationBlocked()
    {
        lock (_sync)
            if (_blockedNavigation)
                throw new CaptureFailedException("Weiterleitungen werden nicht automatisch archiviert.");
    }

    private void ThrowIfNetworkBlocked()
    {
        lock (_sync)
            if (_blockedNetworkHost is not null)
                throw new CaptureFailedException(
                    $"Das Netzwerkziel '{_blockedNetworkHost}' ist nicht öffentlich erreichbar oder nicht zulässig.");
    }

    private void BlockNetworkTarget(string url)
    {
        var host = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : "unbekannt";
        lock (_sync)
            _blockedNetworkHost ??= host;
    }

    private bool IsSourceUrl(string url) =>
        UrlNormalizer.TryNormalizeSource(url, out var normalized, out _) &&
        string.Equals(normalized, _sourceUrl, StringComparison.Ordinal);

    // Caller holds _sync.
    private bool IsAllowedNavigation(IRequest request, bool mainFrame) => mainFrame
        ? (IsSourceUrl(request.Url) && (request.RedirectedFrom is null || _challengeObserved)) ||
          (_challengeObserved && (IsSameOriginChallenge(request.Url) || IsSourceChallengeContinuation(request.Url)))
        : true;

    private bool IsAllowedRedirectTarget(string url, bool navigationRequest, bool mainFrame)
    {
        lock (_sync)
            return !navigationRequest || !mainFrame ||
                _challengeObserved &&
                (IsSourceUrl(url) || IsSameOriginChallenge(url) || IsSourceChallengeContinuation(url));
    }

    private bool IsSameOriginChallenge(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        uri.Scheme == _sourceUri.Scheme && uri.Host.Equals(_sourceUri.Host, StringComparison.OrdinalIgnoreCase) &&
        uri.Port == _sourceUri.Port && CloudflareChallengeDetector.IsChallengePath(uri);

    // Managed Challenge temporarily uses history.replaceState and form targets on
    // the original path with __cf_chl_* parameters. Allow only that query delta;
    // the clean capture still has to return to the exact normalized source URL.
    private bool IsSourceChallengeContinuation(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals(_sourceUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !uri.Host.Equals(_sourceUri.Host, StringComparison.OrdinalIgnoreCase) ||
            uri.Port != _sourceUri.Port ||
            !uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped).Equals(
                _sourceUri.GetComponents(UriComponents.Path, UriFormat.UriEscaped), StringComparison.Ordinal))
            return false;

        var sourceParts = QueryParts(_sourceUri).Order(StringComparer.Ordinal).ToArray();
        var candidateParts = new List<string>();
        var foundChallengeParameter = false;
        foreach (var part in QueryParts(uri))
        {
            var separator = part.IndexOf('=');
            var name = separator < 0 ? part : part[..separator];
            if (Uri.UnescapeDataString(name).StartsWith("__cf_chl_", StringComparison.OrdinalIgnoreCase))
            {
                foundChallengeParameter = true;
                continue;
            }
            candidateParts.Add(part);
        }

        return foundChallengeParameter && sourceParts.SequenceEqual(
            candidateParts.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    private static IEnumerable<string> QueryParts(Uri uri) => uri.Query.TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries);

    public void Dispose()
    {
        _routeRegistration?.Dispose();
        _page.Request -= OnRequest;
        _page.Response -= OnResponse;
        _page.FrameNavigated -= OnFrameNavigated;
    }
}

internal sealed class CaptureChallengeEncounteredException : Exception;
