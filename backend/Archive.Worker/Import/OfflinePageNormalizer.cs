using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Archive.Worker.Import;

internal sealed record NormalizedAsset(string Path, string SourceUrl, string ContentType, byte[] Body);
internal sealed record NormalizedPage(string Html, IReadOnlyList<NormalizedAsset> Assets);

internal static class OfflinePageNormalizer
{
    private static readonly Regex Attribute = new("(?<prefix>\\b(?:src|href|poster|data-src)\\s*=\\s*)(?<quote>[\"'])(?<value>.*?)(?:\\k<quote>)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex CssUrl = new("url\\(\\s*(?<quote>[\"']?)(?<value>[^)\"']+)(?:[\"']?)\\s*\\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CssImport = new("(?<prefix>@import\\s+)(?<quote>[\"'])(?<value>[^\"']+)(?:\\k<quote>)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SourceSet = new("(?<prefix>\\bsrcset\\s*=\\s*)(?<quote>[\"'])(?<value>.*?)(?:\\k<quote>)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex SourceSetUrl = new("(?<url>(?:https?://|cid:|/|\\.\\.?/)[^,\\s]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Iframe = new("<iframe\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public static NormalizedPage Normalize(ImportedPage page, Guid snapshotId)
    {
        var assets = new List<NormalizedAsset>();
        var contentBase = $"/api/snapshots/{snapshotId}/content/";
        var html = NormalizeDocument(page, "root", contentBase, assets, new Dictionary<string, string>(StringComparer.Ordinal));
        return new NormalizedPage(html, assets);
    }

    private static string NormalizeDocument(ImportedPage page, string scope, string contentBase,
        List<NormalizedAsset> assets, IReadOnlyDictionary<string, string> inherited)
    {
        var map = new Dictionary<string, string>(inherited, StringComparer.Ordinal);
        foreach (var resource in page.Resources)
        {
            var path = $"assets/{Hash(scope + ":" + resource.Url)}";
            var local = contentBase + path;
            map[resource.Url] = local;
            foreach (var alias in resource.Aliases ?? [])
                map[alias] = local;
        }

        foreach (var resource in page.Resources)
        {
            var path = $"assets/{Hash(scope + ":" + resource.Url)}";
            var body = resource.Body;
            if (resource.ContentType.StartsWith("text/css", StringComparison.OrdinalIgnoreCase))
            {
                var encoding = GetEncoding(resource.TextEncoding);
                var css = encoding.GetString(body);
                body = Encoding.UTF8.GetBytes(Rewrite(css, resource.Url, map));
            }
            assets.Add(new NormalizedAsset(path, resource.Url, resource.ContentType, body));
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
            var childHtml = NormalizeDocument(child, childScope, contentBase, assets, map);
            assets.Add(new NormalizedAsset(path, child.BaseUrl ?? child.FrameName ?? path,
                "text/html; charset=utf-8", Encoding.UTF8.GetBytes(childHtml)));
            var replacement = contentBase + path;
            return Regex.IsMatch(match.Value, "\\bsrc\\s*=", RegexOptions.IgnoreCase)
                ? Regex.Replace(match.Value, "(?<prefix>\\bsrc\\s*=\\s*)(?<quote>[\"']).*?\\k<quote>",
                    m => $"{m.Groups["prefix"].Value}{m.Groups["quote"].Value}{replacement}{m.Groups["quote"].Value}",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline)
                : match.Value.Insert(match.Value.Length - 1, $" src=\"{replacement}\"");
        });

        html = Regex.Replace(html, "<base\\b[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        var baseTag = $"<base href=\"{contentBase}\">";
        return Regex.Replace(html, "<head\\b[^>]*>", m => m.Value + baseTag, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }

    private static string Rewrite(string text, string? baseUrl, IReadOnlyDictionary<string, string> map)
    {
        foreach (var pair in map.OrderByDescending(x => x.Key.Length))
        {
            text = text.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
            text = text.Replace(pair.Key.Replace("&", "&amp;", StringComparison.Ordinal), pair.Value,
                StringComparison.Ordinal);
        }
        text = Attribute.Replace(text, match => ReplaceValue(match, baseUrl, map));
        text = SourceSet.Replace(text, match =>
        {
            var value = SourceSetUrl.Replace(match.Groups["value"].Value, urlMatch =>
                TryResolve(WebUtility.HtmlDecode(urlMatch.Value), baseUrl, map, out var local)
                    ? local : urlMatch.Value);
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

    private static Encoding GetEncoding(string? name)
    {
        try { return string.IsNullOrWhiteSpace(name) ? new UTF8Encoding(false, true) : Encoding.GetEncoding(name); }
        catch (ArgumentException) { return new UTF8Encoding(false, true); }
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
