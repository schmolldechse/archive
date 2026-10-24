using Archive.Api.Authentication;
using Archive.Api.Mapping;
using Archive.Api.Options;
using Archive.Api.SnapshotContent;
using Archive.Core.Entities;
using Archive.Core.Entities.Processing;
using Archive.Core.Models.Snapshots;
using Archive.Core.Storage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace Archive.Tests;

public sealed class SnapshotContentTests
{
    [Fact]
    public void PublicMapperUsesTheSnapshotOriginContentEndpoint()
    {
        var authentication = AuthenticationOptions();
        var mapper = new SnapshotMapper(new PublicUserMapper(authentication), SnapshotOptions());
        var snapshot = Snapshot();

        Assert.Equal($"http://127.0.0.1:5200/api/snapshots/{snapshot.Id}/content/index.html",
            mapper.ToResponse(snapshot, "http://localhost:5200").ContentUrl);
    }

    [Fact]
    public void PrivateMapperUsesTheSameEndpointWithASnapshotBoundCapability()
    {
        var authentication = AuthenticationOptions();
        var options = SnapshotOptions();
        var publicUsers = new PublicUserMapper(authentication);
        var tokens = new SnapshotContentAccessTokenService(new EphemeralDataProtectionProvider(), options);
        var mapper = new SnapshotSubmissionMapper(new SnapshotMapper(publicUsers, options), publicUsers, options, tokens);
        var snapshot = Snapshot();
        var detail = new SnapshotSubmissionDetail(snapshot,
            new SnapshotModerationDetails(SnapshotModerationStatus.Pending, DateTimeOffset.UtcNow, null),
            ProcessingStatus.Completed, "etag");

        var contentUrl = mapper.ToResponse(detail, "http://localhost:5200/api/users/snapshots/example").Snapshot.ContentUrl;
        var uri = new Uri(contentUrl!);
        var capability = uri.AbsolutePath.Split('/')[5].TrimStart('~');

        Assert.StartsWith($"{options.PublicOrigin}/api/snapshots/{snapshot.Id}/content/~", contentUrl,
            StringComparison.Ordinal);
        Assert.True(tokens.IsValid(snapshot.Id, capability));
    }

    [Fact]
    public void InteractiveDocumentPolicyIsOpaqueSnapshotScopedAndCredentialFree()
    {
        var response = new DefaultHttpContext().Response;
        var snapshotId = Guid.NewGuid();
        var policy = new SnapshotContentPolicy(AuthenticationOptions());
        var scope = $"http://127.0.0.1:5200/api/snapshots/{snapshotId}/content/";

        policy.Apply(response, scope, SnapshotResourceRoles.RootDocument,
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
        Assert.True(policy.IsAllowedDestination(SnapshotResourceRoles.RootDocument, "text/html", "iframe"));
        Assert.False(policy.IsAllowedDestination(SnapshotResourceRoles.Asset, "image/svg+xml", "iframe"));
    }

    [Theory]
    [InlineData("index.html", "text/html", SnapshotResourceRoles.RootDocument)]
    [InlineData("frames/child.html", "text/html; charset=utf-8", SnapshotResourceRoles.NestedDocument)]
    [InlineData("assets/image.svg", "image/svg+xml", SnapshotResourceRoles.Asset)]
    public void SnapshotResourcesReceiveConservativeRoles(string path, string contentType, string expected) =>
        Assert.Equal(expected, SnapshotResourceRoles.Infer(path, contentType));

    [Fact]
    public async Task SnapshotOriginAllowsOnlyTheContentRouteAndStripsCredentials()
    {
        var context = SnapshotContext(HttpMethods.Get,
            $"/api/snapshots/{Guid.NewGuid()}/content/index.html");
        context.Request.Headers.Cookie = "archive.session=secret";
        context.Request.Headers.Authorization = "Bearer secret";
        var reachedEndpoint = false;

        await PublicSnapshotRequestBoundary.ApplyAsync(context, SnapshotOptions(), _ =>
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
    public async Task SnapshotOriginRejectsNonContentAndMutationRoutes(string method, string path)
    {
        var context = SnapshotContext(method, path);
        var reachedEndpoint = false;

        await PublicSnapshotRequestBoundary.ApplyAsync(context, SnapshotOptions(), _ =>
        {
            reachedEndpoint = true;
            return Task.CompletedTask;
        });

        Assert.False(reachedEndpoint);
        Assert.Equal(method == HttpMethods.Post ? StatusCodes.Status405MethodNotAllowed : StatusCodes.Status404NotFound,
            context.Response.StatusCode);
    }

    private static DefaultHttpContext SnapshotContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Host = new HostString("127.0.0.1", 5200);
        context.Request.Path = path;
        return context;
    }

    private static SnapshotContentOptions SnapshotOptions() => new()
    {
        PublicOrigin = "http://127.0.0.1:5200",
        AllowedBrands = ["Chromium"]
    };

    private static ArchiveAuthenticationOptions AuthenticationOptions() => new()
    {
        PublicApiOrigin = "http://localhost:5200",
        FrontendOrigin = "http://localhost:5173"
    };

    private static SnapshotDetails Snapshot() => new(
        Guid.NewGuid(), SourceType.Url, "https://example.test/", "Example", null,
        SnapshotQuality.Complete, DateTimeOffset.UtcNow, 10, 100, 2, [], null,
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
}
