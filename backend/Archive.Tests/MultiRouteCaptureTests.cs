using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Archive.Core.Entities;
using Archive.Core.Options;
using Archive.Core.Processing;
using Archive.Core.Storage;
using Archive.Worker.Capture.Browser;
using Archive.Worker.Capture.Network;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;

namespace Archive.Tests;

[Collection(PlaywrightCollection.Name)]
public sealed class MultiRouteCaptureTests
{
    [Fact]
    public async Task CapturesReachableSameOriginRoutesIntoOneManifest()
    {
        var port = FreeTcpPort();
        var origin = $"http://127.0.0.1:{port}";
        var externalOrigin = $"http://localhost:{port}";
        var externalRequests = 0;
        var store = new MemoryObjectStore();
        var snapshotId = Guid.NewGuid();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(origin);
        await using var app = builder.Build();
        app.MapGet("/", () => Results.Content($$"""
            <!doctype html><html><head><title>Root page</title><link rel="icon" href="data:,"></head><body>
            <a id="a" href="/a#section">A</a>
            <a id="old" href="/old">Old A</a>
            <a id="b" href="/b?view=full">B</a>
            <a id="external" href="{{externalOrigin}}/external">External</a>
            <img src="/shared.png" alt="shared">
            </body></html>
            """, "text/html; charset=utf-8"));
        app.MapGet("/a", () => Results.Content("""
            <!doctype html><html><head><title>Section A</title><link rel="icon" href="data:,"></head><body>
            <h1 id="section">Route A</h1><a href="/b?view=full">B</a><a href="/c">C</a><a href="/">Home</a>
            </body></html>
            """, "text/html; charset=utf-8"));
        app.MapGet("/old", () => Results.Redirect("/a"));
        app.MapGet("/b", (HttpRequest request) => Results.Content($$"""
            <!doctype html><html><head><title>Section B</title><link rel="icon" href="data:,"></head><body>
            <h1>Route B {{request.Query["view"]}}</h1><a href="/">Home</a>
            </body></html>
            """, "text/html; charset=utf-8"));
        app.MapGet("/c", () => Results.Content("""
            <!doctype html><html><head><title>Section C</title><link rel="icon" href="data:,"></head><body>
            <h1>Route C</h1><a href="/a">A</a>
            </body></html>
            """, "text/html; charset=utf-8"));
        app.MapGet("/shared.png", () => Results.Bytes([1, 2, 3, 4], "image/png"));
        app.MapGet("/external", () =>
        {
            Interlocked.Increment(ref externalRequests);
            return Results.Content("external", "text/plain");
        });
        app.MapGet("/snapshot/{**path}", async (string? path) =>
        {
            var stored = await store.TryReadAsync($"snapshots/{snapshotId:N}/{path ?? "index.html"}");
            return stored is null
                ? Results.NotFound()
                : Results.Bytes(stored.Value.Body, stored.Value.ContentType);
        });

        await app.StartAsync();
        try
        {
            var engine = CreateEngine(store, maxLinkedPages: 10);
            var result = await engine.CaptureAsync(
                new CaptureInput(snapshotId, SourceType.Url, origin, null), null, CancellationToken.None);

            Assert.Equal(SnapshotQuality.Complete, result.Quality);
            Assert.Equal(0, Volatile.Read(ref externalRequests));
            var manifest = await store.ReadJsonAsync<SnapshotManifest>($"snapshots/{snapshotId:N}/manifest.json");
            Assert.Equal(SnapshotManifest.CurrentVersion, manifest.Version);
            Assert.False(manifest.Incomplete);
            Assert.Equal(4, manifest.Resources.Count(resource =>
                resource.Role is SnapshotResourceRoles.RootDocument or SnapshotResourceRoles.NestedDocument));
            Assert.Contains(manifest.Resources, resource => resource.SourceUrl == $"{origin}/a" &&
                resource.Role == SnapshotResourceRoles.NestedDocument);
            Assert.Contains(manifest.Resources, resource => resource.SourceUrl == $"{origin}/b?view=full" &&
                resource.Role == SnapshotResourceRoles.NestedDocument);
            Assert.Contains(manifest.Resources, resource => resource.SourceUrl == $"{origin}/c" &&
                resource.Role == SnapshotResourceRoles.NestedDocument);

            var rootHtml = Encoding.UTF8.GetString(await store.ReadBytesAsync(
                $"snapshots/{snapshotId:N}/index.html"));
            Assert.Contains("pages/", rootHtml, StringComparison.Ordinal);
            Assert.Contains("#section", rootHtml, StringComparison.Ordinal);

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var page = await browser.NewPageAsync();
            await page.GotoAsync($"{origin}/snapshot/index.html");
            await page.Locator("#a").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.Contains("/snapshot/pages/", page.Url, StringComparison.Ordinal);
            Assert.EndsWith("#section", page.Url, StringComparison.Ordinal);
            Assert.Equal("Route A", await page.Locator("h1").TextContentAsync());
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task MarksSnapshotIncompleteWhenLinkedPageLimitOmitsRoute()
    {
        var port = FreeTcpPort();
        var origin = $"http://127.0.0.1:{port}";
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(origin);
        await using var app = builder.Build();
        app.MapGet("/", () => Results.Content("""
            <!doctype html><html><head><link rel="icon" href="data:,"></head><body>
            <a href="/a">A</a><a href="/b">B</a>
            </body></html>
            """, "text/html; charset=utf-8"));
        app.MapGet("/a", () => Results.Content(
            "<!doctype html><html><head></head><body>Route A</body></html>", "text/html; charset=utf-8"));
        app.MapGet("/b", () => Results.Content(
            "<!doctype html><html><head></head><body>Route B</body></html>", "text/html; charset=utf-8"));

        await app.StartAsync();
        try
        {
            var store = new MemoryObjectStore();
            var snapshotId = Guid.NewGuid();
            var result = await CreateEngine(store, maxLinkedPages: 1).CaptureAsync(
                new CaptureInput(snapshotId, SourceType.Url, origin, null), null, CancellationToken.None);

            Assert.Equal(SnapshotQuality.Incomplete, result.Quality);
            var manifest = await store.ReadJsonAsync<SnapshotManifest>($"snapshots/{snapshotId:N}/manifest.json");
            Assert.True(manifest.Incomplete);
            Assert.Equal(1, manifest.MissingUrlCount);
            Assert.Contains($"{origin}/b", manifest.MissingUrls);
            Assert.Equal(2, manifest.Resources.Count(resource =>
                resource.Role is SnapshotResourceRoles.RootDocument or SnapshotResourceRoles.NestedDocument));
            var rootHtml = Encoding.UTF8.GetString(await store.ReadBytesAsync(
                $"snapshots/{snapshotId:N}/index.html"));
            Assert.Contains("href=\"/b\"", rootHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("index.htmlb", rootHtml, StringComparison.Ordinal);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static BrowserCaptureEngine CreateEngine(IArchiveObjectStore store, int maxLinkedPages) => new(
        store,
        new ArchiveOptions
        {
            MaxDurationSeconds = 60,
            MaxStorageBytes = 10_000_000,
            MaxResources = 100,
            MaxScrollRounds = 1,
            QuietPeriodMilliseconds = 10
        },
        new ArchiveBrowserOptions
        {
            Headless = true,
            UserAgent = "ArchiveBot.Tests/1.0",
            ChallengeTimeoutSeconds = 5,
            ChallengeReloadAttempts = 1,
            MaxLinkedPages = maxLinkedPages
        },
        new AllowAllNetworkGuard(),
        NullLogger<BrowserCaptureEngine>.Instance);

    private sealed class AllowAllNetworkGuard : ICaptureNetworkGuard
    {
        public Task<bool> IsAllowedAsync(string url, CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class MemoryObjectStore : IArchiveObjectStore
    {
        private readonly ConcurrentDictionary<string, StoredObject> _objects = new(StringComparer.Ordinal);

        public async Task PutAsync(string key, Stream content, string contentType,
            CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            _objects[key] = new(buffer.ToArray(), contentType);
        }

        public Task<ArchiveObject?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!_objects.TryGetValue(key, out var stored))
                return Task.FromResult<ArchiveObject?>(null);
            return Task.FromResult<ArchiveObject?>(new ArchiveObject(
                new MemoryStream(stored.Body, writable: false), stored.ContentType, stored.Body.LongLength));
        }

        public Task<IReadOnlyList<ArchiveObjectInfo>> ListAsync(string prefix,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ArchiveObjectInfo>>(
                _objects.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal))
                    .Select(pair => new ArchiveObjectInfo(pair.Key, pair.Value.Body.LongLength)).ToArray());

        public Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
        {
            var source = _objects[sourceKey];
            _objects[destinationKey] = new(source.Body.ToArray(), source.ContentType);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            _objects.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<byte[]> ReadBytesAsync(string key) => Task.FromResult(_objects[key].Body.ToArray());

        public Task<(byte[] Body, string ContentType)?> TryReadAsync(string key)
        {
            if (!_objects.TryGetValue(key, out var stored))
                return Task.FromResult<(byte[], string)?>(null);
            return Task.FromResult<(byte[], string)?>((stored.Body.ToArray(), stored.ContentType));
        }

        public async Task<T> ReadJsonAsync<T>(string key) where T : notnull =>
            JsonSerializer.Deserialize<T>(await ReadBytesAsync(key))
                ?? throw new InvalidDataException($"Stored object {key} did not contain valid JSON.");

        private sealed record StoredObject(byte[] Body, string ContentType);
    }

    private static int FreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
