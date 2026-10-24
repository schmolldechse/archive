using Archive.Worker.Capture.Replay;

namespace Archive.Tests;

public sealed class ReplayDocumentNormalizerTests
{
    [Fact]
    public void NormalizesOneCanonicalDocumentWithoutRemovingArchivedScripts()
    {
        const string html = """
            <!doctype html><html><head>
            <meta http-equiv="Content-Security-Policy" content="default-src 'none'">
            <script type="module" integrity="sha256-old" crossorigin="use-credentials">
              import "https://example.test/app.js";
            </script></head><body><button onclick="run()">Run</button></body></html>
            """;
        var normalized = ReplayDocumentNormalizer.Normalize(html, "https://example.test/",
            new Dictionary<string, string> { ["https://example.test/app.js"] = "assets/app.js" }, "index.html");

        Assert.True(normalized.ContainsExecutableContent);
        Assert.Contains("data-archive-replay=\"v1\"", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("import \"./assets/app.js\"", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("onclick=\"run()\"", normalized.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Content-Security-Policy", normalized.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("integrity=", normalized.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("crossorigin=", normalized.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RTCPeerConnection", normalized.Html, StringComparison.Ordinal);
        Assert.True(normalized.Html.IndexOf("data-archive-replay", StringComparison.Ordinal) <
            normalized.Html.IndexOf("type=\"module\"", StringComparison.Ordinal));
    }

    [Fact]
    public void RewritesSupportedJavaScriptLiteralForms()
    {
        const string source = """
            import "./module.js";
            export { value } from "./value.js";
            import("./lazy.js");
            new URL("./model.bin", import.meta.url);
            fetch("./data.json");
            """;
        var map = new Dictionary<string, string>
        {
            ["https://example.test/module.js"] = "module.local.js",
            ["https://example.test/value.js"] = "value.local.js",
            ["https://example.test/lazy.js"] = "lazy.local.js",
            ["https://example.test/model.bin"] = "model.local.bin",
            ["https://example.test/data.json"] = "data.local.json"
        };

        var rewritten = JavaScriptReferenceRewriter.Rewrite(source, "https://example.test/app.js", map);

        foreach (var local in map.Values)
            Assert.Contains("./" + local, rewritten, StringComparison.Ordinal);
    }
}
