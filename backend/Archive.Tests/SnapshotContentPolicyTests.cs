using Archive.Api.Authentication;
using Archive.Api.SnapshotContent;
using Archive.Core.Storage;
using Microsoft.AspNetCore.Http;

namespace Archive.Tests;

public sealed class SnapshotContentPolicyTests
{
    [Fact]
    public void InteractiveDocumentsPermitOnlyBlobWorkerEntry()
    {
        var response = new DefaultHttpContext().Response;
        var policy = CreatePolicy();
        const string scope = "http://127.0.0.1:5200/api/snapshots/example/content/";

        policy.Apply(response, scope, SnapshotResourceRoles.RootDocument,
            "text/html; charset=utf-8", interactive: true);

        var contentSecurityPolicy = response.Headers.ContentSecurityPolicy.ToString();
        Assert.Contains("worker-src blob:", contentSecurityPolicy, StringComparison.Ordinal);
        Assert.Contains("'wasm-unsafe-eval'", contentSecurityPolicy, StringComparison.Ordinal);
        Assert.DoesNotContain("'unsafe-eval'", contentSecurityPolicy, StringComparison.Ordinal);
        Assert.DoesNotContain($"worker-src {scope}", contentSecurityPolicy, StringComparison.Ordinal);
        Assert.Contains(scope, response.Headers["Connection-Allowlist"].ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void StaticDocumentsKeepWorkersDisabled()
    {
        var response = new DefaultHttpContext().Response;

        CreatePolicy().Apply(response, "http://127.0.0.1:5200/content/",
            SnapshotResourceRoles.RootDocument, "text/html", interactive: false);

        Assert.Contains("worker-src 'none'", response.Headers.ContentSecurityPolicy.ToString(),
            StringComparison.Ordinal);
        Assert.DoesNotContain("'wasm-unsafe-eval'", response.Headers.ContentSecurityPolicy.ToString(),
            StringComparison.Ordinal);
        Assert.False(response.Headers.ContainsKey("Connection-Allowlist"));
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
            SnapshotResourceRoles.RootDocument, "text/html", interactive: true);

        var permissionsPolicy = response.Headers["Permissions-Policy"].ToString();
        Assert.DoesNotContain("ambient-light-sensor", permissionsPolicy, StringComparison.Ordinal);
        Assert.Contains("camera=()", permissionsPolicy, StringComparison.Ordinal);
        Assert.Contains("microphone=()", permissionsPolicy, StringComparison.Ordinal);
    }

    [Fact]
    public void VersionTwoNormalizerInstallsDedicatedWorkerAdapterBeforeArchivedScripts()
    {
        var normalized = SnapshotDocumentNormalizer.Normalize(
            "<!doctype html><html><head><script>window.archived = true;</script></head><body></body></html>",
            "https://source.example/index.html", new Dictionary<string, string>(), "index.html");

        Assert.Equal("archive-snapshot-v2", SnapshotDocumentNormalizer.Version);
        Assert.Contains("data-archive-snapshot=\"v2\"", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("const ArchivedWorker = new Proxy", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("navigator.serviceWorker", normalized.Html, StringComparison.Ordinal);
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
