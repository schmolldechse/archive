using System.Text;
using Archive.Core.Storage;
using Archive.SnapshotMigrator;

namespace Archive.Tests;

public sealed class SnapshotMigrationNormalizerTests
{
    [Fact]
    public void NormalizesLegacyHtmlWithTheCanonicalSnapshotRuntime()
    {
        var resources = new[]
        {
            new SnapshotMigrationResourceDescriptor(
                "index.html", "https://example.test/page.html", "text/html; charset=utf-8"),
            new SnapshotMigrationResourceDescriptor(
                "assets/app-hash", "https://example.test/app.js", "application/javascript"),
            new SnapshotMigrationResourceDescriptor(
                "assets/data-hash", "https://example.test/data.json", "application/json")
        };
        const string html = """
            <!doctype html><html><head>
            <meta http-equiv="Content-Security-Policy" content="default-src 'none'">
            <script src="assets/app-hash"></script>
            </head><body><button onclick="run()">Run</button></body></html>
            """;

        var sourceMap = SnapshotMigrationNormalizer.BuildSourceMap(resources);
        var normalized = SnapshotMigrationNormalizer.Normalize(
            resources[0], Encoding.UTF8.GetBytes(html), sourceMap);
        var result = Encoding.UTF8.GetString(normalized.Body);

        Assert.DoesNotContain("https://example.test/page.html", sourceMap.Keys);
        Assert.Equal("text/html; charset=utf-8", normalized.ContentType);
        Assert.Equal(SnapshotResourceRoles.RootDocument, normalized.Role);
        Assert.True(normalized.ContainsExecutableContent);
        Assert.Contains($"data-archive-snapshot=\"{SnapshotDocumentNormalizer.Version[^2..]}\"", result,
            StringComparison.Ordinal);
        Assert.Contains("https://example.test/data.json", result, StringComparison.Ordinal);
        Assert.Contains("assets/data-hash", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Content-Security-Policy", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RewritesLegacyJavaScriptReadsToSiblingSnapshotResources()
    {
        var resources = new[]
        {
            new SnapshotMigrationResourceDescriptor(
                "assets/app-hash", "https://example.test/scripts/app.js", "text/javascript"),
            new SnapshotMigrationResourceDescriptor(
                "assets/data-hash", "https://example.test/scripts/data.json", "application/json")
        };

        var normalized = SnapshotMigrationNormalizer.Normalize(
            resources[0], Encoding.UTF8.GetBytes("fetch('./data.json');"),
            SnapshotMigrationNormalizer.BuildSourceMap(resources));

        Assert.Equal("text/javascript; charset=utf-8", normalized.ContentType);
        Assert.Equal(SnapshotResourceRoles.Asset, normalized.Role);
        Assert.True(normalized.ContainsExecutableContent);
        Assert.Equal("fetch('./data-hash');", Encoding.UTF8.GetString(normalized.Body));
    }

    [Fact]
    public void AssignsNestedDocumentRoleWhileKeepingSnapshotRelativePaths()
    {
        var resources = new[]
        {
            new SnapshotMigrationResourceDescriptor(
                "frames/child.html", "https://example.test/frame/", "text/html"),
            new SnapshotMigrationResourceDescriptor(
                "assets/frame-script", "https://example.test/frame/app.js", "text/javascript")
        };
        const string html = "<html><head></head><body><script src=\"../assets/frame-script\"></script></body></html>";

        var normalized = SnapshotMigrationNormalizer.Normalize(
            resources[0], Encoding.UTF8.GetBytes(html),
            SnapshotMigrationNormalizer.BuildSourceMap(resources));
        var result = Encoding.UTF8.GetString(normalized.Body);

        Assert.Equal(SnapshotResourceRoles.NestedDocument, normalized.Role);
        Assert.Contains("<base href=\"../\">", result, StringComparison.Ordinal);
        Assert.Contains("src=\"../assets/frame-script\"", result, StringComparison.Ordinal);
    }
}
