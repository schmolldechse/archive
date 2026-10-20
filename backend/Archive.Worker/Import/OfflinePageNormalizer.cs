using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Archive.Core.Storage;

namespace Archive.Worker.Import;

internal sealed record NormalizedAsset(string Path, string SourceUrl, string ContentType, byte[] Body);
internal sealed record NormalizedPage(string Html, IReadOnlyList<NormalizedAsset> Assets);

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
        var html = NormalizeDocument(page, "root", "index.html", assets,
            new Dictionary<string, string>(StringComparer.Ordinal));
        return new NormalizedPage(html, assets);
    }

    private static string NormalizeDocument(ImportedPage page, string scope, string documentPath,
        List<NormalizedAsset> assets, IReadOnlyDictionary<string, string> inherited)
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
            assets.Add(new NormalizedAsset(path, resource.Url, contentType, body));
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
            var childHtml = NormalizeDocument(child, childScope, path, assets, map);
            assets.Add(new NormalizedAsset(path, child.BaseUrl ?? child.FrameName ?? path,
                "text/html; charset=utf-8", Encoding.UTF8.GetBytes(childHtml)));
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
            return Regex.Replace(html, "<head\\b[^>]*>", m => m.Value + baseTag,
                RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        if (Regex.IsMatch(html, "<html\\b", RegexOptions.IgnoreCase))
            return Regex.Replace(html, "<html\\b[^>]*>", m => m.Value + "<head>" + baseTag + "</head>",
                RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        return "<head>" + baseTag + "</head>" + html;
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
}
