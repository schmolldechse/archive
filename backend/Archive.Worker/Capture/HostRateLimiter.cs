using System.Collections.Concurrent;

namespace Archive.Worker.Capture;

public sealed class HostRateLimiter(CrawlPolicyOptions options)
{
    private readonly ConcurrentDictionary<string, HostState> _hosts = new(StringComparer.OrdinalIgnoreCase);

    public async Task WaitAsync(Uri target, TimeSpan robotsDelay, CancellationToken cancellationToken)
    {
        var key = target.GetLeftPart(UriPartial.Authority);
        var state = _hosts.GetOrAdd(key, static _ => new HostState());
        var delay = robotsDelay > options.MinimumHostDelay ? robotsDelay : options.MinimumHostDelay;

        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            var remaining = state.LastRequestAt + delay - DateTimeOffset.UtcNow;
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining, cancellationToken);
            state.LastRequestAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            state.Gate.Release();
        }
    }

    private sealed class HostState
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public DateTimeOffset LastRequestAt { get; set; } = DateTimeOffset.MinValue;
    }
}
