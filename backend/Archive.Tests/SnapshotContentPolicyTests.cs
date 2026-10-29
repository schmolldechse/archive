using Archive.Api.Authentication;
using Archive.Api.SnapshotContent;
using Archive.Core.Storage;
using Microsoft.AspNetCore.Http;

namespace Archive.Tests;

public sealed class SnapshotContentPolicyTests
{
    [Fact]
    public void DocumentsPermitOnlyCapturedBlobWorkerBootstrap()
    {
        var response = new DefaultHttpContext().Response;
        var policy = CreatePolicy();
        const string scope = "http://127.0.0.1:5200/api/snapshots/example/content/";

        policy.Apply(response, scope, SnapshotResourceRoles.RootDocument,
            "text/html; charset=utf-8");

        var contentSecurityPolicy = response.Headers.ContentSecurityPolicy.ToString();
        Assert.Contains("worker-src blob:", contentSecurityPolicy, StringComparison.Ordinal);
        Assert.Contains("'wasm-unsafe-eval'", contentSecurityPolicy, StringComparison.Ordinal);
        Assert.DoesNotContain("'unsafe-eval'", contentSecurityPolicy, StringComparison.Ordinal);
        Assert.DoesNotContain($"worker-src {scope}", contentSecurityPolicy, StringComparison.Ordinal);
        Assert.Contains($"frame-src {scope};", contentSecurityPolicy, StringComparison.Ordinal);
        Assert.DoesNotContain($"frame-src {scope} data:", contentSecurityPolicy, StringComparison.Ordinal);
        Assert.Contains(scope, response.Headers["Connection-Allowlist"].ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://127.0.0.1:5200/api/snapshots/example/content/")]
    [InlineData("http://127.0.0.1:5200/api/snapshots/example/content/~capability/")]
    public void PublicAndCapabilityDocumentsUseTheSamePolicy(string scope)
    {
        var response = new DefaultHttpContext().Response;

        CreatePolicy().Apply(response, scope, SnapshotResourceRoles.RootDocument, "text/html");

        Assert.Contains($"script-src {scope}", response.Headers.ContentSecurityPolicy.ToString(),
            StringComparison.Ordinal);
        Assert.Equal($"(\"{scope}*\")", response.Headers["Connection-Allowlist"].ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\"Safari\";v=\"18\"")]
    [InlineData("\"Firefox\";v=\"145\"")]
    [InlineData("malformed")]
    [InlineData("\"Chromium\";v=\"152\"")]
    public void ClientHintsDoNotChangeDocumentPlayback(string? clientHint)
    {
        var context = new DefaultHttpContext();
        if (clientHint is not null)
            context.Request.Headers["Sec-CH-UA"] = clientHint;
        const string scope = "http://127.0.0.1:5200/api/snapshots/example/content/";

        CreatePolicy().Apply(context.Response, scope,
            SnapshotResourceRoles.RootDocument, "text/html");

        Assert.Contains($"script-src {scope}", context.Response.Headers.ContentSecurityPolicy.ToString(),
            StringComparison.Ordinal);
        Assert.Contains("sandbox allow-scripts", context.Response.Headers.ContentSecurityPolicy.ToString(),
            StringComparison.Ordinal);
        Assert.Contains(scope, context.Response.Headers["Connection-Allowlist"].ToString(),
            StringComparison.Ordinal);
        Assert.False(context.Response.Headers.ContainsKey("Vary"));
        Assert.Equal("off", context.Response.Headers["X-DNS-Prefetch-Control"].ToString());
    }

    [Theory]
    [InlineData("sharedworker")]
    [InlineData("serviceworker")]
    public void PersistentWorkerDestinationsAreRejected(string destination) =>
        Assert.False(CreatePolicy().IsAllowedDestination(
            SnapshotResourceRoles.Asset, "text/javascript", destination));

    [Fact]
    public void UnsupportedAmbientLightDirectiveIsNotEmitted()
    {
        var response = new DefaultHttpContext().Response;

        CreatePolicy().Apply(response, "http://127.0.0.1:5200/content/",
            SnapshotResourceRoles.RootDocument, "text/html");

        var permissionsPolicy = response.Headers["Permissions-Policy"].ToString();
        Assert.DoesNotContain("ambient-light-sensor", permissionsPolicy, StringComparison.Ordinal);
        Assert.Contains("camera=()", permissionsPolicy, StringComparison.Ordinal);
        Assert.Contains("microphone=()", permissionsPolicy, StringComparison.Ordinal);
    }

    [Fact]
    public void VersionThreeNormalizerReplacesOlderShimBeforeArchivedScripts()
    {
        var normalized = SnapshotDocumentNormalizer.Normalize(
            "<!doctype html><html><head><link rel=PreConnect href=https://live.example>" +
            "<script data-archive-snapshot=v2>window.legacyShim = true;</script>" +
            "<script>window.archived = true;</script></head><body></body></html>",
            "https://source.example/index.html", new Dictionary<string, string>(), "index.html");

        Assert.Equal("archive-snapshot-v3", SnapshotDocumentNormalizer.Version);
        Assert.Contains("data-archive-snapshot=\"v3\"", normalized.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("legacyShim", normalized.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("preconnect", normalized.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("const ArchivedWorker = new Proxy", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("navigator.serviceWorker", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("sendBeacon", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("blocked an arbitrary Blob worker", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("\"SharedWorker\"", normalized.Html, StringComparison.Ordinal);
        Assert.True(normalized.Html.IndexOf("data-archive-snapshot", StringComparison.Ordinal) <
            normalized.Html.IndexOf("window.archived", StringComparison.Ordinal));
    }

    private static SnapshotContentPolicy CreatePolicy() => new(new ArchiveAuthenticationOptions
    {
        PublicApiOrigin = "http://localhost:5200",
        FrontendOrigin = "http://localhost:5173"
    });
}
