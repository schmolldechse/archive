using System.Collections.Concurrent;
using Microsoft.Playwright;

namespace Archive.Worker.Capture.Browser.Access;

// Context-level routing sees the first request of popups as well. Only pages
// explicitly registered by the capture engine are allowed to reach the network.
internal sealed class BrowserRequestRouter(IBrowserContext context) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<IPage, Func<IRoute, Task>> _handlers =
        new(ReferenceEqualityComparer.Instance);

    public async Task InitializeAsync()
    {
        await context.RouteWebSocketAsync("**/*", socket => _ = socket.CloseAsync());
        await context.RouteAsync("**/*", async route =>
        {
            IPage? page;
            try { page = route.Request.Frame.Page; }
            catch (PlaywrightException) { page = null; }

            if (page is null || !_handlers.TryGetValue(page, out var handler))
            {
                await route.AbortAsync();
                return;
            }

            await handler(route);
        });
    }

    public IDisposable Register(IPage page, Func<IRoute, Task> handler)
    {
        if (!_handlers.TryAdd(page, handler))
            throw new InvalidOperationException("Für die Browserseite ist bereits eine Netzwerkrichtlinie registriert.");
        return new Registration(_handlers, page);
    }

    public Task<IAPIResponse> FetchAsync(IRoute route, IEnumerable<KeyValuePair<string, string>>? headers = null)
        => route.FetchAsync(new RouteFetchOptions { MaxRedirects = 0, Headers = headers });

    public ValueTask DisposeAsync()
    {
        _handlers.Clear();
        return ValueTask.CompletedTask;
    }

    private sealed class Registration(ConcurrentDictionary<IPage, Func<IRoute, Task>> handlers, IPage page) : IDisposable
    {
        public void Dispose() => handlers.TryRemove(page, out _);
    }
}
