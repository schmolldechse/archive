using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Archive.Core.Storage;
using Archive.Worker.Capture.Replay;

namespace Archive.Worker.Import;

internal sealed record NormalizedAsset(string Path, string SourceUrl, string ContentType, byte[] Body, string Role);
internal sealed record NormalizedPage(string Html, IReadOnlyList<NormalizedAsset> Assets,
    int? ReplayVersion, bool ContainsExecutableContent, string? ReplayDiagnostic);

internal static class OfflinePageNormalizer
{
    private static readonly Regex Attribute = new("(?<prefix>\\b(?:src|href|poster|data-[\\w-]+)\\s*=\\s*)(?<quote>[\"'])(?<value>.*?)(?:\\k<quote>)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex CssUrl = new("url\\(\\s*(?<quote>[\"']?)(?<value>[^)\"']+)(?:[\"']?)\\s*\\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CssImport = new("(?<prefix>@import\\s+)(?<quote>[\"'])(?<value>[^\"']+)(?:\\k<quote>)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SourceSet = new("(?<prefix>\\b(?:data-)?srcset\\s*=\\s*)(?<quote>[\"'])(?<value>.*?)(?:\\k<quote>)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Iframe = new("<iframe\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public static NormalizedPage Normalize(ImportedPage page)
    {
        var assets = new List<NormalizedAsset>();
        var state = new ReplayNormalizationState();
        var html = NormalizeDocument(page, "root", "index.html", assets,
            new Dictionary<string, string>(StringComparer.Ordinal), state);
        return new NormalizedPage(html, assets,
            state.Compatible ? SnapshotReplayManifest.CurrentReplayVersion : null,
            state.ContainsExecutableContent, state.Diagnostic);
    }

    private static string NormalizeDocument(ImportedPage page, string scope, string documentPath,
        List<NormalizedAsset> assets, IReadOnlyDictionary<string, string> inherited,
        ReplayNormalizationState state)
    {
        var map = new Dictionary<string, string>(inherited, StringComparer.Ordinal);
        foreach (var resource in page.Resources)
        {
            var path = $"assets/{Hash(scope + ":" + resource.Url)}";
            map[resource.Url] = path;
            foreach (var alias in resource.Aliases ?? [])
                map[alias] = path;
        }

        foreach (var resource in page.Resources)
        {
            var path = $"assets/{Hash(scope + ":" + resource.Url)}";
            var body = resource.Body;
            var contentType = resource.ContentType;
            if (resource.ContentType.StartsWith("text/css", StringComparison.OrdinalIgnoreCase))
            {
                var css = SnapshotTextEncoding.Decode(body, resource.ContentType, out _, resource.TextEncoding);
                var cssMap = map.ToDictionary(pair => pair.Key,
                    pair => SnapshotResourcePath.RelativeTo(path, pair.Value), StringComparer.Ordinal);
                body = Encoding.UTF8.GetBytes(Rewrite(css, resource.Url, cssMap));
                contentType = "text/css; charset=utf-8";
            }
            else if (resource.ContentType.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
                resource.ContentType.Contains("ecmascript", StringComparison.OrdinalIgnoreCase))
            {
                var script = SnapshotTextEncoding.Decode(body, resource.ContentType, out _, resource.TextEncoding);
                var scriptMap = map.ToDictionary(pair => pair.Key,
                    pair => SnapshotResourcePath.RelativeTo(path, pair.Value), StringComparer.Ordinal);
                body = Encoding.UTF8.GetBytes(JavaScriptReferenceRewriter.Rewrite(script, resource.Url, scriptMap));
                contentType = "text/javascript; charset=utf-8";
                state.ContainsExecutableContent = true;
            }
            assets.Add(new NormalizedAsset(path, resource.Url, contentType, body,
                SnapshotReplayResourceRoles.Asset));
        }

        var html = Rewrite(page.Html, page.BaseUrl, map);
        var frameIndex = 0;
        html = Iframe.Replace(html, match =>
        {
            if (frameIndex >= page.Frames.Count)
                return match.Value;
            var child = page.Frames[frameIndex];
            var path = $"frames/{scope}-{frameIndex}.html";
            var childScope = $"{scope}-{frameIndex}";
            frameIndex++;
            var childHtml = NormalizeDocument(child, childScope, path, assets, map, state);
            assets.Add(new NormalizedAsset(path, child.BaseUrl ?? child.FrameName ?? path,
                "text/html; charset=utf-8", Encoding.UTF8.GetBytes(childHtml),
                SnapshotReplayResourceRoles.NestedDocument));
            var replacement = path;
            return Regex.IsMatch(match.Value, "\\bsrc\\s*=", RegexOptions.IgnoreCase)
                ? Regex.Replace(match.Value, "(?<prefix>\\bsrc\\s*=\\s*)(?<quote>[\"']).*?\\k<quote>",
                    m => $"{m.Groups["prefix"].Value}{m.Groups["quote"].Value}{replacement}{m.Groups["quote"].Value}",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline)
                : match.Value.Insert(match.Value.Length - 1, $" src=\"{replacement}\"");
        });

        html = Regex.Replace(html, "<base\\b[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        var baseTag = $"<base href=\"{SnapshotResourcePath.HtmlBaseFor(documentPath)}\">";
        if (Regex.IsMatch(html, "<head\\b", RegexOptions.IgnoreCase))
            html = Regex.Replace(html, "<head\\b[^>]*>", m => m.Value + baseTag,
                RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        else if (Regex.IsMatch(html, "<html\\b", RegexOptions.IgnoreCase))
            html = Regex.Replace(html, "<html\\b[^>]*>", m => m.Value + "<head>" + baseTag + "</head>",
                RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        else
            html = "<head>" + baseTag + "</head>" + html;

        if (!state.Compatible)
            return html;
        try
        {
            var documentMap = map.ToDictionary(pair => pair.Key,
                pair => SnapshotResourcePath.RelativeTo(documentPath, pair.Value), StringComparer.Ordinal);
            var replay = ReplayDocumentNormalizer.Normalize(html, page.BaseUrl, documentMap, documentPath);
            state.ContainsExecutableContent |= replay.ContainsExecutableContent;
            return replay.Html;
        }
        catch (Exception exception)
        {
            // Imported source still receives the established static normalization when replay parsing fails.
            state.Compatible = false;
            state.Diagnostic ??= exception.GetType().Name;
            return html;
        }
    }

    private static string Rewrite(string text, string? baseUrl, IReadOnlyDictionary<string, string> map)
    {
        baseUrl = SnapshotResourcePath.ResolveDocumentBase(text, baseUrl);
        foreach (var pair in map.OrderByDescending(x => x.Key.Length))
        {
            text = text.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
            text = text.Replace(pair.Key.Replace("&", "&amp;", StringComparison.Ordinal), pair.Value,
                StringComparison.Ordinal);
        }
        text = Attribute.Replace(text, match => ReplaceValue(match, baseUrl, map));
        text = SourceSet.Replace(text, match =>
        {
            var localPaths = map.Values.ToHashSet(StringComparer.Ordinal);
            var value = SnapshotSourceSet.Rewrite(match.Groups["value"].Value, candidate =>
            {
                var decoded = WebUtility.HtmlDecode(candidate);
                if (localPaths.Contains(decoded))
                    return decoded;
                return TryResolve(decoded, baseUrl, map, out var local) ? local : null;
            });
            var quote = match.Groups["quote"].Value;
            return $"{match.Groups["prefix"].Value}{quote}{value}{quote}";
        });
        text = CssUrl.Replace(text, match =>
        {
            var value = match.Groups["value"].Value.Trim();
            return TryResolve(value, baseUrl, map, out var local) ? $"url('{local}')" : match.Value;
        });
        return CssImport.Replace(text, match =>
        {
            var value = match.Groups["value"].Value;
            return TryResolve(value, baseUrl, map, out var local)
                ? $"{match.Groups["prefix"].Value}'{local}'" : match.Value;
        });
    }

    private static string ReplaceValue(Match match, string? baseUrl, IReadOnlyDictionary<string, string> map)
    {
        var value = WebUtility.HtmlDecode(match.Groups["value"].Value);
        if (!TryResolve(value, baseUrl, map, out var local))
            return match.Value;
        var quote = match.Groups["quote"].Value;
        return $"{match.Groups["prefix"].Value}{quote}{local}{quote}";
    }

    private static bool TryResolve(string value, string? baseUrl, IReadOnlyDictionary<string, string> map,
        out string local)
    {
        if (map.TryGetValue(value, out local!))
            return true;
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) &&
            Uri.TryCreate(baseUri, value, out var absolute) && map.TryGetValue(absolute.ToString(), out local!))
            return true;
        local = string.Empty;
        return false;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class ReplayNormalizationState
    {
        public bool Compatible { get; set; } = true;
        public bool ContainsExecutableContent { get; set; }
        public string? Diagnostic { get; set; }
    }
}
