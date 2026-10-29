using System.Net;
using System.Net.Sockets;
using Archive.Worker.Capture.Browser;
using Archive.Worker.Capture.Network;
using Archive.Worker.Import;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Archive.Tests;

public sealed class ImportedMediaCompleterTests
{
    [Fact]
    public async Task CompletesExplicitFiniteMediaAndSupportAssets()
    {
        var origin = $"http://127.0.0.1:{FreeTcpPort()}";
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(origin);
        await using var app = builder.Build();
        app.MapGet("/video.mp4", () => Results.Bytes([1, 2, 3], "video/mp4"));
        app.MapGet("/audio.mp3", () => Results.Bytes([4, 5], "audio/mpeg"));
        app.MapGet("/poster.jpg", () => Results.Bytes([6], "image/jpeg"));
        app.MapGet("/captions.vtt", () => Results.Text("WEBVTT\n", "text/vtt"));
        await app.StartAsync();
        try
        {
            using var completer = CreateCompleter();
            var page = new ImportedPage($$"""
                <!doctype html><html><body>
                <video poster="/poster.jpg"><source src="/video.mp4"><track src="/captions.vtt"></video>
                <audio src="/audio.mp3"></audio>
                </body></html>
                """, $"{origin}/page", [], []);

            var result = await completer.CompleteAsync(page, 1_000, 10, CancellationToken.None);

            Assert.Empty(result.MissingUrls);
            Assert.Collection(result.Page.Resources.OrderBy(resource => resource.Url, StringComparer.Ordinal),
                resource => Assert.Equal("audio/mpeg", resource.ContentType),
                resource => Assert.Equal("text/vtt", resource.ContentType),
                resource => Assert.Equal("image/jpeg", resource.ContentType),
                resource => Assert.Equal("video/mp4", resource.ContentType));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task RejectsPartialMediaCompletion()
    {
        var origin = $"http://127.0.0.1:{FreeTcpPort()}";
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(origin);
        await using var app = builder.Build();
        app.MapGet("/partial.mp4", async context =>
        {
            context.Response.StatusCode = StatusCodes.Status206PartialContent;
            context.Response.ContentType = "video/mp4";
            context.Response.Headers.ContentRange = "bytes 0-1/10";
            await context.Response.Body.WriteAsync(new byte[] { 1, 2 });
        });
        await app.StartAsync();
        try
        {
            using var completer = CreateCompleter();
            var page = new ImportedPage("<video src='/partial.mp4'></video>", $"{origin}/page", [], []);

            var result = await completer.CompleteAsync(page, 1_000, 10, CancellationToken.None);

            Assert.Empty(result.Page.Resources);
            Assert.Equal([$"{origin}/partial.mp4"], result.MissingUrls);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task RejectsMediaThatExceedsTheRemainingStorageBudget()
    {
        var origin = $"http://127.0.0.1:{FreeTcpPort()}";
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(origin);
        await using var app = builder.Build();
        app.MapGet("/large.mp4", () => Results.Bytes([1, 2, 3], "video/mp4"));
        await app.StartAsync();
        try
        {
            using var completer = CreateCompleter();
            var page = new ImportedPage("<video src='/large.mp4'></video>", $"{origin}/page", [], []);

            var result = await completer.CompleteAsync(page, 2, 1, CancellationToken.None);

            Assert.Empty(result.Page.Resources);
            Assert.Equal([$"{origin}/large.mp4"], result.MissingUrls);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static ImportedMediaCompleter CreateCompleter() => new(new AllowAllNetworkGuard(),
        new ArchiveBrowserOptions { UserAgent = "ArchiveBot.Tests/1.0" },
        NullLogger<ImportedMediaCompleter>.Instance);

    private static int FreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class AllowAllNetworkGuard : ICaptureNetworkGuard
    {
        public Task<bool> IsAllowedAsync(string url, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
