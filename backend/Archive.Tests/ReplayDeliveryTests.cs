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
    public void PublicMapperKeepsOneContentUrlAndSelectsReplayFromStoredVersion()
    {
        var authentication = AuthenticationOptions();
        var mapper = new SnapshotMapper(new PublicUserMapper(authentication), ReplayOptions());
        var compatible = Snapshot(replayVersion: SnapshotReplayManifest.CurrentReplayVersion);
        var legacy = Snapshot(replayVersion: null);

        Assert.Equal($"http://127.0.0.1:5200/replay/snapshots/{compatible.Id}/index.html",
            mapper.ToResponse(compatible, "http://localhost:5200").ContentUrl);
        Assert.Equal($"http://localhost:5200/api/snapshots/{legacy.Id}/content/index.html",
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
        Assert.Contains($"/replay/snapshots/{snapshotId}/", response.Headers["Connection-Allowlist"].ToString(),
            StringComparison.Ordinal);
        Assert.True(policy.IsAllowedDestination(SnapshotReplayResourceRoles.RootDocument, "text/html", "iframe"));
        Assert.False(policy.IsAllowedDestination(SnapshotReplayResourceRoles.Asset, "image/svg+xml", "iframe"));
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
