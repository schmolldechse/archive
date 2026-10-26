using System.Text.Json.Nodes;
using Archive.Core.Storage;

namespace Archive.Tests;

public sealed class SnapshotResourceUrlTests
{
    private const string SourceUrl = "https://de.wikipedia.org/wiki/Berlin";
    private const string ResourceUrl =
        "https://de.wikipedia.org/w/load.php?lang=de&modules=ext.cite.styles%7Cskins.vector.styles%2Flegacy&only=styles";

    [Fact]
    public void AbsoluteUrlKeyPreservesEscapedQueryDataAndRemovesFragment()
    {
        var url = new Uri(ResourceUrl + "#theme");

        var key = SnapshotResourcePath.ToAbsoluteUrlKey(url);

        Assert.Equal(ResourceUrl, key);
        Assert.Contains("%7C", key, StringComparison.Ordinal);
        Assert.Contains("%2F", key, StringComparison.Ordinal);
        Assert.DoesNotContain("|", key, StringComparison.Ordinal);
        Assert.DoesNotContain("#theme", key, StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentNormalizerRewritesPercentEncodedStylesheetReference()
    {
        const string html = """
            <!doctype html>
            <html><head><link rel="stylesheet" href="/w/load.php?lang=de&amp;modules=ext.cite.styles%7Cskins.vector.styles%2Flegacy&amp;only=styles#theme"></head><body></body></html>
            """;
        var resources = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ResourceUrl] = "assets/main.css"
        };

        var normalized = SnapshotDocumentNormalizer.Normalize(html, SourceUrl, resources, "index.html");

        Assert.Contains("href=\"assets/main.css\"", normalized.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<link rel=\"stylesheet\" href=\"/w/load.php", normalized.Html,
            StringComparison.Ordinal);
    }

    [Fact]
    public void JavaScriptRewriterUsesEscapedAbsoluteResourceKey()
    {
        var resources = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ResourceUrl] = "assets/main.css"
        };

        var rewritten = JavaScriptReferenceRewriter.Rewrite(
            "fetch('/w/load.php?lang=de&modules=ext.cite.styles%7Cskins.vector.styles%2Flegacy&only=styles#theme')",
            SourceUrl, resources);

        Assert.Equal("fetch('./assets/main.css')", rewritten);
    }

    [Fact]
    public void ImportMapRewriterUsesEscapedAbsoluteResourceKey()
    {
        const string importMap = """
            {"imports":{"styles":"/w/load.php?lang=de&modules=ext.cite.styles%7Cskins.vector.styles%2Flegacy&only=styles#theme"}}
            """;
        var resources = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ResourceUrl] = "assets/main.css"
        };

        var rewritten = SnapshotImportMapRewriter.Rewrite(importMap, SourceUrl, resources);
        var imports = JsonNode.Parse(rewritten)!["imports"]!;

        Assert.Equal("./assets/main.css", imports["styles"]!.GetValue<string>());
    }
}
