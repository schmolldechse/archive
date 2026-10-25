using Archive.Worker.Capture.Snapshots;

namespace Archive.Tests;

public sealed class SnapshotDocumentNormalizerTests
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
        var normalized = SnapshotDocumentNormalizer.Normalize(html, "https://example.test/",
            new Dictionary<string, string> { ["https://example.test/app.js"] = "assets/app.js" }, "index.html");

        Assert.True(normalized.ContainsExecutableContent);
        Assert.Contains("data-archive-snapshot=\"v1\"", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("import \"./assets/app.js\"", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("onclick=\"run()\"", normalized.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Content-Security-Policy", normalized.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("integrity=", normalized.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("crossorigin=", normalized.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RTCPeerConnection", normalized.Html, StringComparison.Ordinal);
        Assert.True(normalized.Html.IndexOf("data-archive-snapshot", StringComparison.Ordinal) <
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

    [Fact]
    public void ExpandsCapturedImportMapPrefixesIntoExactSnapshotSpecifiers()
    {
        const string html = """
            <!doctype html><html><head>
            <script type="importmap">
              { "imports": { "three": "../build/three.module.js", "three/addons/": "./jsm/" } }
            </script>
            <script type="module">
              import * as THREE from "three";
              import GUI from "three/addons/libs/lil-gui.module.min.js";
            </script></head><body></body></html>
            """;
        var normalized = SnapshotDocumentNormalizer.Normalize(html,
            "https://threejs.org/examples/webgl_animation.html",
            new Dictionary<string, string>
            {
                ["https://threejs.org/build/three.module.js"] = "assets/three-hash",
                ["https://threejs.org/examples/jsm/libs/lil-gui.module.min.js"] = "assets/gui-hash"
            }, "index.html");

        Assert.Contains("\"three\": \"./assets/three-hash\"", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("\"three/addons/libs/lil-gui.module.min.js\": \"./assets/gui-hash\"",
            normalized.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void MapsImportMapScopesToCapturedModuleLocations()
    {
        const string html = """
            <!doctype html><html><head>
            <script type="importmap">
              { "scopes": { "./features/": { "library": "./vendor/library.js" } } }
            </script></head><body></body></html>
            """;
        var normalized = SnapshotDocumentNormalizer.Normalize(html, "https://example.test/app/index.html",
            new Dictionary<string, string>
            {
                ["https://example.test/app/features/main.js"] = "assets/main-hash.js",
                ["https://example.test/app/vendor/library.js"] = "assets/library-hash.js"
            }, "index.html");

        Assert.Contains("\"./assets/main-hash.js\"", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("\"library\": \"./assets/library-hash.js\"", normalized.Html,
            StringComparison.Ordinal);
        Assert.DoesNotContain("\"./features/\"", normalized.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void InstallsRuntimeCompatibilityForLegacyScriptLoaders()
    {
        const string html = """
            <!doctype html><html><head><script src="../tdl/base.js"></script></head>
            <body><img src="assets/fish.png"></body></html>
            """;
        var normalized = SnapshotDocumentNormalizer.Normalize(html,
            "https://webglsamples.org/aquarium/aquarium.html",
            new Dictionary<string, string>
            {
                ["https://webglsamples.org/tdl/base.js"] = "assets/base-hash",
                ["https://webglsamples.org/tdl/sync.js"] = "assets/sync-hash",
                ["https://webglsamples.org/aquarium/assets/fish.png"] = "assets/fish-hash"
            }, "index.html");

        Assert.Contains("sourceByLocal", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("localUrl.startsWith(localRoot)", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("Document.prototype.write", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("HTMLScriptElement", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("src=\"assets/base-hash\"", normalized.Html, StringComparison.Ordinal);
        Assert.Contains("src=\"assets/fish-hash\" crossorigin=\"anonymous\"", normalized.Html,
            StringComparison.Ordinal);
        Assert.Contains("candidate.protocol === \"blob:\"", normalized.Html, StringComparison.Ordinal);
    }
}
