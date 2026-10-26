using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
namespace Archive.Core.Storage;

/// <summary>A normalized canonical document and its executable-content diagnostic.</summary>
public sealed record SnapshotDocument(string Html, bool ContainsExecutableContent);

/// <summary>Creates the single canonical HTML document consumed by static and interactive delivery.</summary>
public static class SnapshotDocumentNormalizer
{
    /// <summary>Identifier persisted when this canonical normalization contract is applied.</summary>
    public const string Version = "archive-snapshot-v2";

    private static readonly string[] UrlAttributes = ["src", "href", "poster", "action"];

    /// <summary>Normalizes an archived HTML document for isolated snapshot delivery.</summary>
    public static SnapshotDocument Normalize(string html, string? sourceUrl,
        IReadOnlyDictionary<string, string> map, string documentPath)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);
        var baseUrl = SnapshotResourcePath.ResolveDocumentBase(html, sourceUrl);

        foreach (var existingShim in document.QuerySelectorAll("script[data-archive-snapshot]"))
            existingShim.Remove();

        foreach (var meta in document.QuerySelectorAll("meta[http-equiv], meta[name]"))
        {
            var httpEquiv = meta.GetAttribute("http-equiv");
            var name = meta.GetAttribute("name");
            if (httpEquiv is not null && (httpEquiv.Equals("content-security-policy", StringComparison.OrdinalIgnoreCase) ||
                    httpEquiv.Equals("refresh", StringComparison.OrdinalIgnoreCase)) ||
                name?.Equals("referrer", StringComparison.OrdinalIgnoreCase) == true)
                meta.Remove();
        }

        foreach (var element in document.QuerySelectorAll("[integrity], [crossorigin], [referrerpolicy], [ping]"))
        {
            element.RemoveAttribute("integrity");
            element.RemoveAttribute("crossorigin");
            element.RemoveAttribute("referrerpolicy");
            element.RemoveAttribute("ping");
        }
        foreach (var link in document.QuerySelectorAll("link[rel]"))
        {
            var rel = link.GetAttribute("rel")?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
            if (rel.Any(value => value is "preconnect" or "dns-prefetch" or "prefetch" or "prerender"))
                link.Remove();
        }

        foreach (var oldBase in document.QuerySelectorAll("base"))
            oldBase.Remove();
        foreach (var element in document.All)
        {
            foreach (var attribute in UrlAttributes)
            {
                var value = element.GetAttribute(attribute);
                if (value is not null && TryResolve(value, baseUrl, map, out var local))
                {
                    element.SetAttribute(attribute, local);
                    if (attribute == "src" && element.LocalName is "img" or "audio" or "video")
                        element.SetAttribute("crossorigin", "anonymous");
                }
            }

            var sourceSet = element.GetAttribute("srcset");
            if (sourceSet is not null)
                element.SetAttribute("srcset", SnapshotSourceSet.Rewrite(sourceSet,
                    candidate => TryResolve(candidate, baseUrl, map, out var local) ? local : null));
        }

        var head = document.Head ?? throw new InvalidDataException("The HTML document has no head element.");
        var baseElement = document.CreateElement("base");
        baseElement.SetAttribute("href", SnapshotResourcePath.HtmlBaseFor(documentPath));
        head.InsertBefore(baseElement, head.FirstChild);

        var containsExecutableContent = document.QuerySelector("script") is not null ||
            document.All.Any(element => element.Attributes.Any(attribute =>
                attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase)));
        var documentMap = BuildAbsoluteMap(baseUrl, map);
        foreach (var script in document.QuerySelectorAll("script"))
        {
            var type = script.GetAttribute("type");
            if (type?.Equals("importmap", StringComparison.OrdinalIgnoreCase) == true)
                script.TextContent = SnapshotImportMapRewriter.Rewrite(script.TextContent, baseUrl, documentMap);
            else if (type?.Equals("module", StringComparison.OrdinalIgnoreCase) == true &&
                script.GetAttribute("src") is null)
                script.TextContent = JavaScriptReferenceRewriter.Rewrite(script.TextContent, baseUrl, documentMap);
        }

        var shim = document.CreateElement("script");
        shim.SetAttribute("data-archive-snapshot", "v2");
        shim.TextContent = SnapshotRuntimeShim.Build(baseUrl, documentMap);
        head.InsertBefore(shim, baseElement.NextSibling);

        var serialized = document.DocumentElement?.OuterHtml
            ?? throw new InvalidDataException("The HTML document has no document element.");
        if (document.Doctype is not null)
            serialized = "<!doctype html>" + serialized;
        return new SnapshotDocument(serialized, containsExecutableContent);
    }

    private static IReadOnlyDictionary<string, string> BuildAbsoluteMap(string? baseUrl,
        IReadOnlyDictionary<string, string> map)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in map)
        {
            if (Uri.TryCreate(pair.Key, UriKind.Absolute, out var absolute))
                result[SnapshotResourcePath.ToAbsoluteUrlKey(absolute)] = pair.Value;
            else if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var documentBase) &&
                Uri.TryCreate(documentBase, pair.Key, out absolute))
                result[SnapshotResourcePath.ToAbsoluteUrlKey(absolute)] = pair.Value;
        }
        return result;
    }

    private static bool TryResolve(string value, string? baseUrl, IReadOnlyDictionary<string, string> map,
        out string local)
    {
        var decoded = WebUtility.HtmlDecode(value);
        if (map.TryGetValue(decoded, out local!))
            return true;
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var documentBase) &&
            Uri.TryCreate(documentBase, decoded, out var absolute) &&
            map.TryGetValue(SnapshotResourcePath.ToAbsoluteUrlKey(absolute), out local!))
            return true;
        local = string.Empty;
        return false;
    }

}
