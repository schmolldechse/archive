using System.Net;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Archive.Core.Storage;

namespace Archive.Worker.Capture.Snapshots;

internal sealed record SnapshotDocument(string Html, bool ContainsExecutableContent);

/// <summary>Creates the single canonical HTML document consumed by static and interactive delivery.</summary>
internal static class SnapshotDocumentNormalizer
{
    private static readonly string[] UrlAttributes = ["src", "href", "poster", "action"];

    public static SnapshotDocument Normalize(string html, string? sourceUrl,
        IReadOnlyDictionary<string, string> map, string documentPath)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);
        var baseUrl = SnapshotResourcePath.ResolveDocumentBase(html, sourceUrl);

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
                    element.SetAttribute(attribute, local);
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
        foreach (var script in document.QuerySelectorAll("script[type='module']:not([src])"))
            script.TextContent = JavaScriptReferenceRewriter.Rewrite(script.TextContent, baseUrl, documentMap);

        var shim = document.CreateElement("script");
        shim.SetAttribute("data-archive-snapshot", "v1");
        shim.TextContent = BuildShim(baseUrl, documentMap);
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
                result[WithoutFragment(absolute).ToString()] = pair.Value;
            else if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var documentBase) &&
                Uri.TryCreate(documentBase, pair.Key, out absolute))
                result[WithoutFragment(absolute).ToString()] = pair.Value;
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
            map.TryGetValue(WithoutFragment(absolute).ToString(), out local!))
            return true;
        local = string.Empty;
        return false;
    }

    private static Uri WithoutFragment(Uri uri) => string.IsNullOrEmpty(uri.Fragment)
        ? uri
        : new UriBuilder(uri) { Fragment = string.Empty }.Uri;

    private static string BuildShim(string? sourceUrl, IReadOnlyDictionary<string, string> map)
    {
        var serializedMap = JsonSerializer.Serialize(map);
        var serializedBase = JsonSerializer.Serialize(sourceUrl);
        return $$"""
            (() => {
              "use strict";
              const originalBase = {{serializedBase}} || "about:blank";
              const resources = Object.freeze({{serializedMap}});
              const nativeFetch = window.fetch.bind(window);
              const resolve = (input) => {
                const absolute = new URL(String(input), originalBase);
                absolute.hash = "";
                const local = resources[absolute.href];
                if (!local) throw new TypeError("The archived snapshot blocked an uncaptured resource.");
                return new URL(local, document.baseURI).href;
              };
              window.fetch = (input, init = {}) => {
                const request = input instanceof Request ? input : null;
                const method = String(init.method || request?.method || "GET").toUpperCase();
                if (method !== "GET" && method !== "HEAD")
                  return Promise.reject(new TypeError("The archived snapshot permits only GET and HEAD."));
                const url = resolve(request?.url || input);
                return nativeFetch(url, { ...init, method, credentials: "omit", redirect: "error", referrerPolicy: "no-referrer" });
              };
              const nativeOpen = XMLHttpRequest.prototype.open;
              XMLHttpRequest.prototype.open = function(method, url, ...rest) {
                const normalized = String(method).toUpperCase();
                if (normalized !== "GET" && normalized !== "HEAD")
                  throw new TypeError("The archived snapshot permits only GET and HEAD.");
                return nativeOpen.call(this, normalized, resolve(url), ...rest);
              };
              for (const name of ["WebSocket", "EventSource", "WebTransport", "Worker", "SharedWorker", "RTCPeerConnection", "webkitRTCPeerConnection"])
                try { Object.defineProperty(window, name, { value: class { constructor() { throw new TypeError("The archived snapshot blocked this capability."); } } }); } catch {}
              window.open = () => null;
              addEventListener("submit", event => event.preventDefault(), true);
            })();
            """;
    }
}
