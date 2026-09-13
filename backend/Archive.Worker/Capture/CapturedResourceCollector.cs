using Archive.Core.Jobs;
using Microsoft.Playwright;

namespace Archive.Worker.Capture;

internal sealed class CapturedResourceCollector : IAsyncDisposable
{
    private readonly IPage _page;
    private readonly CaptureLimits _limits;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;
    private readonly object _gate = new();
    private readonly Dictionary<string, CapturedResource> _resources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _failures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _successes = new(StringComparer.Ordinal);
    private readonly List<Task> _captureTasks = [];
    private bool _accepting = true;
    private long _sequence;
    private long _capturedBytes;
    private Task? _stopTask;

    public CapturedResourceCollector(IPage page, CaptureLimits limits, ILogger logger, CancellationToken cancellationToken)
    {
        _page = page;
        _limits = limits;
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
                return new Dictionary<string, CapturedResource>(_resources, StringComparer.Ordinal);
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
                return _failures.Count > 0;
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

    public ValueTask DisposeAsync() => new(StopAsync());

    private void OnResponse(object? sender, IResponse response)
    {
        lock (_gate)
        {
            if (!_accepting)
                return;

            // Retain every task, including faulted tasks and duplicate URLs: a later
            // successful response must never hide an earlier resource-limit failure.
            _captureTasks.Add(CaptureResponseAsync(response, ++_sequence));
        }
    }

    private void OnRequestFailed(object? sender, IRequest request)
    {
        lock (_gate)
        {
            if (!_accepting || string.IsNullOrWhiteSpace(request.Url) ||
                CloudflareChallengeDetector.IsChallengeResource(request.Url) || IsMainDocument(request))
                return;

            RecordFailure(request.Url, ++_sequence);
            _logger.LogDebug("Ressourcenanfrage {Url} fehlgeschlagen: {Failure}", request.Url, request.Failure);
        }
    }

    private async Task CaptureResponseAsync(IResponse response, long sequence)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(response.Url) ||
                CloudflareChallengeDetector.IsChallengeResource(response.Url) || IsMainDocument(response.Request))
                return;

            if (CloudflareChallengeDetector.IsChallengeResponse(response) || response.Status >= 400)
            {
                lock (_gate)
                    RecordFailure(response.Url, sequence);
                return;
            }

            if (response.Status < 200 || response.Status >= 300 || response.Status is 204 or 205 ||
                string.Equals(response.Request.Method, "HEAD", StringComparison.OrdinalIgnoreCase))
                return;

            var contentType = response.Headers.TryGetValue("content-type", out var header)
                ? header.Split(';', 2)[0].Trim()
                : "application/octet-stream";
            if (!IsArchivableContentType(contentType))
                return;

            _cancellationToken.ThrowIfCancellationRequested();
            var body = await response.BodyAsync().WaitAsync(_cancellationToken).ConfigureAwait(false);
            _cancellationToken.ThrowIfCancellationRequested();

            lock (_gate)
            {
                if (!_resources.ContainsKey(response.Url))
                {
                    if (_resources.Count >= _limits.MaxResources)
                        throw new CaptureDiscardedException("Das Ressourcenlimit wurde überschritten.");
                    if (body.LongLength > _limits.MaxStorageBytes - _capturedBytes)
                        throw new CaptureDiscardedException("Das Speicherlimit wurde überschritten.");

                    _resources.Add(response.Url, new CapturedResource(response.Url, contentType, body));
                    _capturedBytes += body.LongLength;
                }

                RecordSuccess(response.Url, sequence);
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
                RecordFailure(response.Url, sequence);
            _logger.LogDebug(exception, "Ressource {Url} konnte nicht gespeichert werden", response.Url);
        }
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

    // Called only while holding _gate. Sequence numbers follow browser events rather
    // than body completion order, so a slow earlier failure cannot undo a retry.
    private void RecordFailure(string url, long sequence)
    {
        if (_successes.TryGetValue(url, out var success) && success >= sequence)
            return;
        if (!_failures.TryGetValue(url, out var failure) || failure < sequence)
            _failures[url] = sequence;
    }

    private void RecordSuccess(string url, long sequence)
    {
        if (!_successes.TryGetValue(url, out var success) || success < sequence)
            _successes[url] = sequence;
        if (_failures.TryGetValue(url, out var failure) && failure <= sequence)
            _failures.Remove(url);
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
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("font/", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
}
