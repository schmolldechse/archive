using Archive.Api.Authentication;
using Archive.Api.Mapping;
using Archive.Api.Options;
using Archive.Api.Replay;
using Archive.Core.Entities;
using Archive.Core.Models.Snapshots;
using Archive.Core.Storage;
using Microsoft.AspNetCore.Http;

namespace Archive.Tests;

public sealed class ReplayDeliveryTests
{
    [Fact]
    public void PublicMapperUsesOneReplayOriginContentEndpointForAllGenerations()
    {
        var authentication = AuthenticationOptions();
        var mapper = new SnapshotMapper(new PublicUserMapper(authentication), ReplayOptions());
        var compatible = Snapshot(replayVersion: SnapshotReplayManifest.CurrentReplayVersion);
        var legacy = Snapshot(replayVersion: null);

        Assert.Equal($"http://127.0.0.1:5200/api/snapshots/{compatible.Id}/content/index.html",
            mapper.ToResponse(compatible, "http://localhost:5200").ContentUrl);
        Assert.Equal($"http://127.0.0.1:5200/api/snapshots/{legacy.Id}/content/index.html",
            mapper.ToResponse(legacy, "http://localhost:5200").ContentUrl);
    }

    [Fact]
    public void InteractiveDocumentPolicyIsOpaqueSnapshotScopedAndCredentialFree()
    {
        var response = new DefaultHttpContext().Response;
        var snapshotId = Guid.NewGuid();
        var policy = new ReplayContentPolicy(ReplayOptions(), AuthenticationOptions());

        policy.Apply(response, snapshotId, SnapshotReplayResourceRoles.RootDocument,
            "text/html; charset=utf-8", interactive: true);

        Assert.Equal("*", response.Headers.AccessControlAllowOrigin);
        Assert.Contains("sandbox allow-scripts", response.Headers.ContentSecurityPolicy.ToString(),
            StringComparison.Ordinal);
        Assert.DoesNotContain("allow-same-origin", response.Headers.ContentSecurityPolicy.ToString(),
            StringComparison.Ordinal);
        Assert.DoesNotContain("webrtc", response.Headers.ContentSecurityPolicy.ToString(),
            StringComparison.Ordinal);
        Assert.Contains($"/api/snapshots/{snapshotId}/content/",
            response.Headers["Connection-Allowlist"].ToString(),
            StringComparison.Ordinal);
        Assert.True(policy.IsAllowedDestination(SnapshotReplayResourceRoles.RootDocument, "text/html", "iframe"));
        Assert.False(policy.IsAllowedDestination(SnapshotReplayResourceRoles.Asset, "image/svg+xml", "iframe"));
    }

    [Theory]
    [InlineData("index.html", "text/html", SnapshotReplayResourceRoles.RootDocument)]
    [InlineData("frames/child.html", "text/html; charset=utf-8", SnapshotReplayResourceRoles.NestedDocument)]
    [InlineData("assets/image.svg", "image/svg+xml", SnapshotReplayResourceRoles.Asset)]
    public void LegacyResourcesReceiveConservativeReplayRoles(string path, string contentType, string expected) =>
        Assert.Equal(expected, SnapshotReplayResourceRoles.Infer(path, contentType));

    [Fact]
    public async Task ReplayHostAllowsOnlyTheExistingPublicContentRouteAndStripsCredentials()
    {
        var context = ReplayContext(HttpMethods.Get,
            $"/api/snapshots/{Guid.NewGuid()}/content/index.html");
        context.Request.Headers.Cookie = "archive.session=secret";
        context.Request.Headers.Authorization = "Bearer secret";
        var reachedEndpoint = false;

        await ReplayRequestBoundary.ApplyAsync(context, ReplayOptions(), _ =>
        {
            reachedEndpoint = true;
            return Task.CompletedTask;
        });

        Assert.True(reachedEndpoint);
        Assert.False(context.Request.Headers.ContainsKey("Cookie"));
        Assert.False(context.Request.Headers.ContainsKey("Authorization"));
    }

    [Theory]
    [InlineData("GET", "/api/snapshots/00000000-0000-0000-0000-000000000001")]
    [InlineData("GET", "/api/users/snapshots/00000000-0000-0000-0000-000000000001/content/index.html")]
    [InlineData("POST", "/api/snapshots/00000000-0000-0000-0000-000000000001/content/index.html")]
    public async Task ReplayHostRejectsNonContentAndMutationRoutes(string method, string path)
    {
        var context = ReplayContext(method, path);
        var reachedEndpoint = false;

        await ReplayRequestBoundary.ApplyAsync(context, ReplayOptions(), _ =>
        {
            reachedEndpoint = true;
            return Task.CompletedTask;
        });

        Assert.False(reachedEndpoint);
        Assert.Equal(method == HttpMethods.Post ? StatusCodes.Status405MethodNotAllowed : StatusCodes.Status404NotFound,
            context.Response.StatusCode);
    }

    private static DefaultHttpContext ReplayContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Host = new HostString("127.0.0.1", 5200);
        context.Request.Path = path;
        return context;
    }

    private static ReplayOptions ReplayOptions() => new()
    {
        PublicOrigin = "http://127.0.0.1:5200",
        AllowedBrands = ["Chromium"]
    };

    private static ArchiveAuthenticationOptions AuthenticationOptions() => new()
    {
        PublicApiOrigin = "http://localhost:5200",
        FrontendOrigin = "http://localhost:5173"
    };

    private static SnapshotDetails Snapshot(int? replayVersion) => new(
        Guid.NewGuid(), SourceType.Url, "https://example.test/", "Example", null,
        SnapshotQuality.Complete, DateTimeOffset.UtcNow, 10, 100, 2, replayVersion, [], null,
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
}
