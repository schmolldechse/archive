using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Collections.Concurrent;
using Archive.Api.Authentication;
using Archive.Api.SnapshotContent;
using Archive.Core.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright;

namespace Archive.Tests;

public sealed class SnapshotWorkerReplayTests
{
    [Fact]
    public async Task DedicatedWorkersRunWithoutEscapingTheSnapshotBoundary()
    {
        var port = FreeTcpPort();
        var snapshotOrigin = $"http://127.0.0.1:{port}";
        var viewerOrigin = $"http://localhost:{port}";
        var scope = $"{snapshotOrigin}/snapshot/";
        var resources = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["https://source.example/worker.js"] = "assets/worker.js",
            ["https://source.example/data.json"] = "assets/data.json",
            ["https://source.example/nested-worker.js"] = "assets/nested-worker.js",
            ["https://source.example/module-worker.js"] = "assets/module-worker.js",
            ["https://source.example/module-dependency.js"] = "assets/module-dependency.js"
        };
        var shim = SnapshotRuntimeShim.Build("https://source.example/index.html", resources);
        var sentinelUrl = JsonSerializer.Serialize($"{viewerOrigin}/sentinel");
        var snapshotHtml = $$"""
            <!doctype html><html><head><meta charset="utf-8"><script>{{shim}}</script></head><body>
            <output id="blob">pending</output>
            <output id="classic">pending</output>
            <output id="module">pending</output>
            <output id="external">pending</output>
            <output id="uncaptured">pending</output>
            <output id="shared">pending</output>
            <output id="service">pending</output>
            <script>
              const result = (id, value) => document.getElementById(id).textContent = String(value);

              const blobUrl = URL.createObjectURL(new Blob([
                "self.onmessage = event => postMessage(event.data * 2);"
              ], { type: "text/javascript" }));
              const blobWorker = new Worker(blobUrl);
              URL.revokeObjectURL(blobUrl);
              blobWorker.onmessage = event => result("blob", event.data);
              blobWorker.onerror = () => result("blob", "error");
              blobWorker.postMessage(21);

              const classicWorker = new Worker("https://source.example/worker.js");
              classicWorker.onmessage = event => result("classic", event.data);
              classicWorker.onerror = () => result("classic", "error");
              classicWorker.postMessage(39);

              const moduleWorker = new Worker("https://source.example/module-worker.js", { type: "module" });
              moduleWorker.onmessage = event => result("module", event.data);
              moduleWorker.onerror = event => result("module", event.message || "error");
              moduleWorker.postMessage(null);

              const externalUrl = {{sentinelUrl}};
              const externalBlobUrl = URL.createObjectURL(new Blob([
                `fetch(${JSON.stringify(externalUrl)}).then(() => postMessage("escaped"), () => postMessage("blocked"));`
              ], { type: "text/javascript" }));
              const externalWorker = new Worker(externalBlobUrl);
              URL.revokeObjectURL(externalBlobUrl);
              externalWorker.onmessage = event => result("external", event.data);
              externalWorker.onerror = () => result("external", "error");

              try { new Worker("https://live.example/worker.js"); result("uncaptured", "escaped"); }
              catch { result("uncaptured", "blocked"); }

              const sharedUrl = URL.createObjectURL(new Blob(["onconnect = () => {};"],
                { type: "text/javascript" }));
              try { new SharedWorker(sharedUrl); result("shared", "escaped"); }
              catch { result("shared", "blocked"); }
              finally { URL.revokeObjectURL(sharedUrl); }

              Promise.resolve().then(async () => {
                try {
                  await navigator.serviceWorker.register("{{scope}}assets/service-worker.js");
                  result("service", "escaped");
                } catch { result("service", "blocked"); }
              });
            </script></body></html>
            """;
        var policy = new SnapshotContentPolicy(new ArchiveAuthenticationOptions
        {
            PublicApiOrigin = viewerOrigin,
            FrontendOrigin = viewerOrigin
        });
        var sentinelRequests = 0;
        var serviceWorkerRequests = 0;
        var serverRequests = new ConcurrentQueue<string>();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        await using var app = builder.Build();
        app.Use(async (context, next) =>
        {
            serverRequests.Enqueue($"{context.Request.Path} ({context.Request.Headers["Sec-Fetch-Dest"]})");
            await next(context);
        });
        app.MapGet("/viewer", () => Results.Content(
            $"<!doctype html><iframe sandbox=\"allow-scripts\" src=\"{snapshotOrigin}/snapshot/index.html\"></iframe>",
            "text/html"));
        app.MapGet("/sentinel", () =>
        {
            Interlocked.Increment(ref sentinelRequests);
            return Results.NoContent();
        });
        app.MapGet("/snapshot/index.html", async context =>
        {
            policy.Apply(context.Response, scope, SnapshotResourceRoles.RootDocument,
                "text/html; charset=utf-8", interactive: true);
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(snapshotHtml);
        });
        app.MapGet("/snapshot/assets/worker.js", async context =>
        {
            ApplyAssetPolicy(context, policy, scope, "text/javascript; charset=utf-8");
            await context.Response.WriteAsync($$"""
                self.onmessage = async event => {
                  const data = await (await fetch("https://source.example/data.json")).json();
                  const blocked = callback => { try { callback(); return false; } catch { return true; } };
                  const guardsHeld =
                    blocked(() => { const xhr = new XMLHttpRequest(); xhr.open("GET", {{sentinelUrl}}); }) &&
                    blocked(() => new WebSocket("ws://localhost:{{port}}/sentinel")) &&
                    blocked(() => new EventSource({{sentinelUrl}})) &&
                    blocked(() => new WebTransport({{sentinelUrl}})) &&
                    blocked(() => new RTCPeerConnection());
                  if (!guardsHeld) { postMessage("escaped"); return; }
                  const nested = new Worker("https://source.example/nested-worker.js");
                  nested.onmessage = nestedEvent => postMessage(event.data + data.increment + nestedEvent.data);
                  nested.onerror = () => postMessage("nested-error");
                  nested.postMessage(null);
                };
                """);
        });
        app.MapGet("/snapshot/assets/data.json", async context =>
        {
            ApplyAssetPolicy(context, policy, scope, "application/json");
            await context.Response.WriteAsync("{\"increment\":2}");
        });
        app.MapGet("/snapshot/assets/nested-worker.js", async context =>
        {
            ApplyAssetPolicy(context, policy, scope, "text/javascript; charset=utf-8");
            await context.Response.WriteAsync("self.onmessage = () => postMessage(1);");
        });
        app.MapGet("/snapshot/assets/module-worker.js", async context =>
        {
            ApplyAssetPolicy(context, policy, scope, "text/javascript; charset=utf-8");
            await context.Response.WriteAsync("""
                import { answer } from "./module-dependency.js";
                self.onmessage = () => postMessage(answer);
                """);
        });
        app.MapGet("/snapshot/assets/module-dependency.js", async context =>
        {
            ApplyAssetPolicy(context, policy, scope, "text/javascript; charset=utf-8");
            await context.Response.WriteAsync("export const answer = 42;");
        });
        app.MapGet("/snapshot/assets/service-worker.js", () =>
        {
            Interlocked.Increment(ref serviceWorkerRequests);
            return Results.Content(string.Empty, "text/javascript");
        });

        await app.StartAsync();
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var page = await browser.NewPageAsync();
            var diagnostics = new ConcurrentQueue<string>();
            page.Console += (_, message) => diagnostics.Enqueue($"console {message.Type}: {message.Text}");
            page.PageError += (_, message) => diagnostics.Enqueue($"page error: {message}");
            page.RequestFailed += (_, request) =>
                diagnostics.Enqueue($"request failed: {request.Url} ({request.Failure})");
            await page.GotoAsync($"{viewerOrigin}/viewer");
            var frame = await WaitForSnapshotFrameAsync(page, snapshotOrigin);
            try
            {
                await frame.WaitForFunctionAsync("""
                    () => ["blob", "classic", "module", "external", "uncaptured", "shared", "service"]
                      .every(id => document.getElementById(id)?.textContent !== "pending")
                    """, null, new() { Timeout = 5_000 });
            }
            catch (TimeoutException)
            {
                // Assertions below include every result and request for an actionable failure.
            }

            var expected = new Dictionary<string, string>
            {
                ["blob"] = "42",
                ["classic"] = "42",
                ["module"] = "42",
                ["external"] = "blocked",
                ["uncaptured"] = "blocked",
                ["shared"] = "blocked",
                ["service"] = "blocked"
            };
            var actual = new Dictionary<string, string?>();
            foreach (var id in expected.Keys)
                actual[id] = await frame.Locator("#" + id).TextContentAsync();
            Assert.True(expected.All(pair => string.Equals(pair.Value, actual[pair.Key], StringComparison.Ordinal)),
                "Worker results: " + string.Join(", ", actual.Select(pair => $"{pair.Key}={pair.Value}")) +
                Environment.NewLine + string.Join(Environment.NewLine, diagnostics) +
                Environment.NewLine + string.Join(Environment.NewLine, serverRequests));
            Assert.Equal(0, Volatile.Read(ref sentinelRequests));
            Assert.Equal(0, Volatile.Read(ref serviceWorkerRequests));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static void ApplyAssetPolicy(HttpContext context, SnapshotContentPolicy policy,
        string scope, string contentType)
    {
        policy.Apply(context.Response, scope, SnapshotResourceRoles.Asset, contentType, interactive: true);
        context.Response.ContentType = contentType;
    }

    private static async Task<IFrame> WaitForSnapshotFrameAsync(IPage page, string snapshotOrigin)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var frame = page.Frames.SingleOrDefault(candidate =>
                candidate.Url.StartsWith(snapshotOrigin + "/snapshot/", StringComparison.Ordinal));
            if (frame is not null)
                return frame;
            await Task.Delay(50);
        }

        throw new TimeoutException("The isolated snapshot frame did not load.");
    }

    private static int FreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
