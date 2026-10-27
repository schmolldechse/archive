using Archive.Core.Jobs;

namespace Archive.Worker.Capture.Browser.Crawl;

/// <summary>Collects unique resources and request outcomes across every document in one URL capture.</summary>
internal sealed class CapturedResourceArchive(CaptureLimits limits)
{
    private readonly object _gate = new();
    private readonly Dictionary<string, CapturedResource> _resources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _failures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _successes = new(StringComparer.Ordinal);
    private long _sequence;
    private long _capturedBytes;

    public long NextSequence()
    {
        lock (_gate)
            return ++_sequence;
    }

    public void ReserveDocument(long byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        lock (_gate)
        {
            EnsureCapacity(byteCount);
            _capturedBytes += byteCount;
        }
    }

    public void ReleaseDocument(long byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        lock (_gate)
            _capturedBytes = Math.Max(0, _capturedBytes - byteCount);
    }

    public void StoreResource(string url, string contentType, byte[] body, long sequence)
    {
        lock (_gate)
        {
            if (!_resources.ContainsKey(url))
            {
                if (_resources.Count >= limits.MaxResources)
                    throw new CaptureDiscardedException("The resource limit was exceeded.");
                EnsureCapacity(body.LongLength);
                _resources.Add(url, new CapturedResource(url, contentType, body));
                _capturedBytes += body.LongLength;
            }

            RecordSuccessCore(url, sequence);
        }
    }

    public void RecordFailure(string url, long sequence)
    {
        lock (_gate)
        {
            if (_successes.TryGetValue(url, out var success) && success >= sequence)
                return;
            if (!_failures.TryGetValue(url, out var failure) || failure < sequence)
                _failures[url] = sequence;
        }
    }

    public void RemovePromotedDocument(IEnumerable<string> urls)
    {
        lock (_gate)
        {
            foreach (var url in urls.Distinct(StringComparer.Ordinal))
            {
                if (_resources.Remove(url, out var resource))
                    _capturedBytes = Math.Max(0, _capturedBytes - resource.Body.LongLength);
                _failures.Remove(url);
                _successes.Remove(url);
            }
        }
    }

    public IReadOnlyDictionary<string, CapturedResource> SnapshotResources()
    {
        lock (_gate)
            return new Dictionary<string, CapturedResource>(_resources, StringComparer.Ordinal);
    }

    public IReadOnlyList<string> SnapshotFailedUrls()
    {
        lock (_gate)
            return _failures.Keys.Order(StringComparer.Ordinal).ToArray();
    }

    private void RecordSuccessCore(string url, long sequence)
    {
        if (!_successes.TryGetValue(url, out var success) || success < sequence)
            _successes[url] = sequence;
        if (_failures.TryGetValue(url, out var failure) && failure <= sequence)
            _failures.Remove(url);
    }

    private void EnsureCapacity(long byteCount)
    {
        if (byteCount > limits.MaxStorageBytes - _capturedBytes)
            throw new CaptureDiscardedException("The storage limit was exceeded.");
    }
}
