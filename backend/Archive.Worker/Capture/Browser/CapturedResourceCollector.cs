using Archive.Core.Jobs;
using Archive.Worker.Capture.Browser.Access;
using Archive.Worker.Capture.Browser.Crawl;
using Microsoft.Playwright;

namespace Archive.Worker.Capture.Browser;

internal sealed class CapturedResourceCollector : IAsyncDisposable
{
    private readonly IPage _page;
    private readonly CapturedResourceArchive _archive;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;
    private readonly object _gate = new();
    private readonly List<Task> _captureTasks = [];
    private bool _accepting = true;
    private Task? _stopTask;
    private CapturedMainDocument? _mainDocument;
    private long _reservedDocumentBytes;
    private bool _documentCommitted;

    public CapturedResourceCollector(IPage page, CapturedResourceArchive archive, ILogger logger,
        CancellationToken cancellationToken)
    {
        _page = page;
        _archive = archive;
        _logger = logger;
        _cancellationToken = cancellationToken;
        _page.Response += OnResponse;
        _page.RequestFailed += OnRequestFailed;
    }

    public IReadOnlyDictionary<string, CapturedResource> Resources
    {
        get
        {
            lock (_gate)
            {
                EnsureStopped();
                return _archive.SnapshotResources();
            }
        }
    }

    public bool Incomplete
    {
        get
        {
            lock (_gate)
            {
                EnsureStopped();
                return _archive.SnapshotFailedUrls().Count > 0;
            }
        }
    }

    /// <summary>
    /// Gets the resource URLs whose latest observed request failed during capture.
    /// </summary>
    public IReadOnlyList<string> FailedUrls
    {
        get
        {
            lock (_gate)
            {
                EnsureStopped();
                return _archive.SnapshotFailedUrls();
            }
        }
    }

    public CapturedMainDocument? MainDocument
    {
        get
        {
            lock (_gate)
            {
                EnsureStopped();
                return _mainDocument;
            }
        }
    }

    public async Task StopAsync()
    {
        Task drain;
        lock (_gate)
        {
            if (_stopTask is null)
            {
                // The same lock guards event admission and task registration. A callback
                // already queued by Playwright cannot add work after this boundary.
                _accepting = false;
                _page.Response -= OnResponse;
                _page.RequestFailed -= OnRequestFailed;
                _stopTask = Task.WhenAll(_captureTasks);
            }

            drain = _stopTask;
        }

        await drain.ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        try { await StopAsync(); }
        finally { ReleaseUncommittedDocument(); }
    }

    public CapturedMainDocument? CommitMainDocument()
    {
        lock (_gate)
        {
            EnsureStopped();
            _documentCommitted = true;
            return _mainDocument;
        }
    }

    private void OnResponse(object? sender, IResponse response)
    {
        lock (_gate)
        {
            if (!_accepting)
                return;

            // Retain every task, including faulted tasks and duplicate URLs: a later
            // successful response must never hide an earlier resource-limit failure.
            _captureTasks.Add(CaptureResponseAsync(response, _archive.NextSequence()));
        }
    }

    private void OnRequestFailed(object? sender, IRequest request)
    {
        lock (_gate)
        {
            if (!_accepting || string.IsNullOrWhiteSpace(request.Url) ||
                CloudflareChallengeDetector.IsChallengeResource(request.Url) || IsMainDocument(request))
                return;

            _archive.RecordFailure(request.Url, _archive.NextSequence());
            _logger.LogDebug("Ressourcenanfrage {Url} fehlgeschlagen: {Failure}", request.Url, request.Failure);
        }
    }

    private async Task CaptureResponseAsync(IResponse response, long sequence)
    {
        try
        {
            var isMainDocument = IsMainDocument(response.Request);
            if (string.IsNullOrWhiteSpace(response.Url) ||
                CloudflareChallengeDetector.IsChallengeResource(response.Url))
                return;

            if (CloudflareChallengeDetector.IsChallengeResponse(response) || response.Status >= 400)
            {
                lock (_gate)
                    _archive.RecordFailure(response.Url, sequence);
                return;
            }

            if (response.Status < 200 || response.Status >= 300 || response.Status is 204 or 205 ||
                string.Equals(response.Request.Method, "HEAD", StringComparison.OrdinalIgnoreCase))
                return;

            var contentType = response.Headers.TryGetValue("content-type", out var header)
                ? header.Trim()
                : "application/octet-stream";
            if (isMainDocument)
            {
                if (!contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) &&
                    !contentType.StartsWith("application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
                    return;
                _cancellationToken.ThrowIfCancellationRequested();
                var mainBody = await response.BodyAsync().WaitAsync(_cancellationToken).ConfigureAwait(false);
                _cancellationToken.ThrowIfCancellationRequested();
                lock (_gate)
                {
                    if (_reservedDocumentBytes > 0)
                    {
                        _archive.ReleaseDocument(_reservedDocumentBytes);
                        _reservedDocumentBytes = 0;
                    }
                    _archive.ReserveDocument(mainBody.LongLength);
                    _reservedDocumentBytes = mainBody.LongLength;
                    _mainDocument = new(response.Url, contentType, response.Status, mainBody,
                        RedirectChain(response.Request));
                }
                return;
            }
            if (!IsArchivableContentType(contentType))
                return;

            _cancellationToken.ThrowIfCancellationRequested();
            var body = await response.BodyAsync().WaitAsync(_cancellationToken).ConfigureAwait(false);
            _cancellationToken.ThrowIfCancellationRequested();

            lock (_gate)
            {
                _archive.StoreResource(response.Url, contentType, body, sequence);
            }
        }
        catch (CaptureDiscardedException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
                _archive.RecordFailure(response.Url, sequence);
            _logger.LogDebug(exception, "Ressource {Url} konnte nicht gespeichert werden", response.Url);
        }
    }

    private static IReadOnlyList<string> RedirectChain(IRequest request)
    {
        var urls = new Stack<string>();
        for (var current = request.RedirectedFrom; current is not null; current = current.RedirectedFrom)
            urls.Push(current.Url);
        return urls.ToArray();
    }

    private bool IsMainDocument(IRequest request)
    {
        if (!request.IsNavigationRequest)
            return false;

        try
        {
            return request.Frame == _page.MainFrame;
        }
        catch (PlaywrightException)
        {
            // Navigation can fail before Playwright associates a frame. The page's
            // navigation lifecycle handles that failure, not asset collection.
            return true;
        }
    }

    private void EnsureStopped()
    {
        if (_stopTask is not { IsCompletedSuccessfully: true })
            throw new InvalidOperationException("Die Ressourcenaufnahme muss zuerst erfolgreich beendet werden.");
    }

    private static bool IsArchivableContentType(string contentType) =>
        contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
        contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
        contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
        contentType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("application/wasm", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("model/", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("font/", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

    private void ReleaseUncommittedDocument()
    {
        lock (_gate)
        {
            if (_documentCommitted || _reservedDocumentBytes == 0)
                return;
            _archive.ReleaseDocument(_reservedDocumentBytes);
            _reservedDocumentBytes = 0;
        }
    }
}
