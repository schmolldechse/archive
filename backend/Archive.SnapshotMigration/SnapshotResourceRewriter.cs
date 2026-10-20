using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Archive.Core.Storage;

namespace Archive.SnapshotMigration;

/// <summary>Replaces legacy snapshot API URLs with paths relative to the archived content.</summary>
internal static partial class SnapshotResourceRewriter
{
    [GeneratedRegex("<base\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex BaseTag();

    [GeneratedRegex("(?<prefix>\\b(?<name>src|href|poster|data-[\\w-]+|srcset|style)\\s*=\\s*)(?<quote>[\\\"'])(?<value>.*?)(?:\\k<quote>)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HtmlAttribute();

    [GeneratedRegex("(?<prefix>@import\\s+)(?<quote>[\\\"'])(?<value>[^\\\"']+)(?:\\k<quote>)",
        RegexOptions.IgnoreCase)]
    private static partial Regex CssImport();

    [GeneratedRegex("url\\(\\s*(?<quote>[\\\"']?)(?<value>[^)\\\"']+)(?:[\\\"']?)\\s*\\)",
        RegexOptions.IgnoreCase)]
    private static partial Regex CssUrl();

    /// <summary>Rewrites a UTF-8 HTML or CSS object, preserving its optional UTF-8 BOM.</summary>
    public static byte[] Rewrite(byte[] body, string contentType, Guid snapshotId, string objectPath,
        string? originalUrl, IReadOnlyDictionary<string, string> resourceObjectPaths)
    {
        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType) || mediaType.MediaType is null)
            throw new InvalidDataException("A text snapshot resource has an invalid Content-Type.");
        var isHtml = mediaType.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase);
        var isCss = mediaType.MediaType.Equals("text/css", StringComparison.OrdinalIgnoreCase);
        if (!isHtml && !isCss) return body;
        var text = SnapshotTextEncoding.Decode(body, contentType, out var preserveUtf8Bom);
        var rewritten = isHtml
            ? RewriteHtml(text, snapshotId, objectPath, originalUrl, resourceObjectPaths)
            : RewriteCss(text, snapshotId, objectPath, originalUrl, resourceObjectPaths);
        return new UTF8Encoding(preserveUtf8Bom, true).GetBytes(rewritten);
    }

    private static string RewriteHtml(string html, Guid snapshotId, string objectPath, string? originalUrl,
        IReadOnlyDictionary<string, string> resourceObjectPaths)
    {
        var documentPath = SnapshotResourcePath.Normalize(objectPath);
        var resolutionUrl = SnapshotResourcePath.ResolveDocumentBase(html, originalUrl);
        var baseTag = $"<base href=\"{SnapshotResourcePath.HtmlBaseFor(documentPath)}\">";
        var resolutionPath = documentPath;
        var baseCount = 0;
        html = BaseTag().Replace(html, _ => baseCount++ == 0 ? baseTag : string.Empty);
        if (baseCount == 0)
        {
            if (Regex.IsMatch(html, "<head\\b", RegexOptions.IgnoreCase))
                html = Regex.Replace(html, "<head\\b[^>]*>", match => match.Value + baseTag,
                    RegexOptions.IgnoreCase);
            else if (Regex.IsMatch(html, "<html\\b", RegexOptions.IgnoreCase))
                html = Regex.Replace(html, "<html\\b[^>]*>", match => match.Value + "<head>" + baseTag + "</head>",
                    RegexOptions.IgnoreCase);
            else
                html = "<head>" + baseTag + "</head>" + html;
        }

        html = Regex.Replace(html, "(?<open><style\\b[^>]*>)(?<css>.*?)(?<close></style\\s*>)", match =>
            match.Groups["open"].Value + RewriteCss(match.Groups["css"].Value, snapshotId, resolutionPath,
                resolutionUrl, resourceObjectPaths) + match.Groups["close"].Value,
            RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(1));

        html = HtmlAttribute().Replace(html, match =>
        {
            var name = match.Groups["name"].Value;
            var raw = match.Groups["value"].Value;
            string rewritten;
            if (name.Equals("style", StringComparison.OrdinalIgnoreCase))
                rewritten = RewriteCss(raw, snapshotId, resolutionPath, resolutionUrl, resourceObjectPaths);
            else if (name.Equals("srcset", StringComparison.OrdinalIgnoreCase) ||
                     name.Equals("data-srcset", StringComparison.OrdinalIgnoreCase))
                rewritten = SnapshotSourceSet.Rewrite(raw, candidate =>
                {
                    if (TryResolve(candidate, snapshotId, resolutionPath, resolutionUrl,
                            resourceObjectPaths, out var url))
                        return url;
                    return resourceObjectPaths.Values.Contains(candidate, StringComparer.Ordinal)
                        ? candidate
                        : null;
                });
            else if (TryResolve(raw, snapshotId, resolutionPath, resolutionUrl, resourceObjectPaths, out var value))
                rewritten = value;
            else
                return match.Value;

            return match.Groups["prefix"].Value + match.Groups["quote"].Value +
                   WebUtility.HtmlEncode(rewritten) + match.Groups["quote"].Value;
        });
        return RewriteLegacyRoutes(html, snapshotId, resolutionPath);
    }

    private static string RewriteLegacyRoutes(string html, Guid snapshotId, string sourcePath)
    {
        var prefixes = new[]
        {
            $"/api/snapshots/{snapshotId:N}/content/",
            $"/api/snapshots/{snapshotId:D}/content/"
        };
        foreach (var prefix in prefixes)
        {
            var pattern = Regex.Escape(prefix) + "(?<path>[^\\s\\\"'<>\\\\),?#]+)(?<suffix>[?#][^\\s\\\"'<>\\\\)]*)?";
            html = Regex.Replace(html, pattern, match =>
            {
                try
                {
                    var path = SnapshotResourcePath.Normalize(Uri.UnescapeDataString(match.Groups["path"].Value));
                    var relative = SnapshotResourcePath.RelativeTo(sourcePath, path);
                    return relative + match.Groups["suffix"].Value;
                }
                catch (ArgumentException)
                {
                    return match.Value;
                }
            }, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }
        return html;
    }

    private static string RewriteCss(string css, Guid snapshotId, string objectPath, string? originalUrl,
        IReadOnlyDictionary<string, string> resourceObjectPaths)
    {
        var stylesheetPath = SnapshotResourcePath.Normalize(objectPath);
        css = CssImport().Replace(css, match =>
            TryResolve(match.Groups["value"].Value, snapshotId, stylesheetPath, originalUrl,
                resourceObjectPaths, out var value)
                ? $"{match.Groups["prefix"].Value}{match.Groups["quote"].Value}{value}{match.Groups["quote"].Value}"
                : match.Value);
        return CssUrl().Replace(css, match =>
        {
            var value = match.Groups["value"].Value.Trim();
            return TryResolve(value, snapshotId, stylesheetPath, originalUrl, resourceObjectPaths, out var relative)
                ? $"url('{relative}')" : match.Value;
        });
    }

    private static bool TryResolve(string rawValue, Guid snapshotId, string sourcePath, string? originalUrl,
        IReadOnlyDictionary<string, string> resourceObjectPaths, out string rewritten)
    {
        var value = WebUtility.HtmlDecode(rawValue).Trim();
        var suffixIndex = value.IndexOfAny(['?', '#']);
        var suffix = suffixIndex >= 0 ? value[suffixIndex..] : string.Empty;
        var lookupValue = suffixIndex >= 0 ? value[..suffixIndex] : value;

        if (TryGetLegacyObjectPath(lookupValue, snapshotId, out var legacyPath))
        {
            rewritten = SnapshotResourcePath.RelativeTo(sourcePath, legacyPath) + suffix;
            return true;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute) &&
            TryGetResourcePath(absolute, resourceObjectPaths, out var absolutePath))
        {
            rewritten = SnapshotResourcePath.RelativeTo(sourcePath, absolutePath) + suffix;
            return true;
        }

        if (Uri.TryCreate(originalUrl, UriKind.Absolute, out var baseUri) &&
            Uri.TryCreate(baseUri, value, out var resolved) &&
            TryGetResourcePath(resolved, resourceObjectPaths, out var relativePath))
        {
            rewritten = SnapshotResourcePath.RelativeTo(sourcePath, relativePath) + suffix;
            return true;
        }

        rewritten = string.Empty;
        return false;
    }

    private static bool TryGetLegacyObjectPath(string value, Guid snapshotId, out string path)
    {
        var prefixes = new[]
        {
            $"/api/snapshots/{snapshotId:N}/content/",
            $"/api/snapshots/{snapshotId:D}/content/"
        };
        foreach (var prefix in prefixes)
        {
            var index = value.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (index < 0) continue;
            try
            {
                path = SnapshotResourcePath.Normalize(Uri.UnescapeDataString(value[(index + prefix.Length)..]));
                return true;
            }
            catch (ArgumentException) { }
        }
        path = string.Empty;
        return false;
    }

    private static bool TryGetResourcePath(Uri url, IReadOnlyDictionary<string, string> resourceObjectPaths,
        out string path)
    {
        if (resourceObjectPaths.TryGetValue(url.ToString(), out path!)) return true;
        var withoutFragment = new UriBuilder(url) { Fragment = string.Empty }.Uri.ToString();
        if (resourceObjectPaths.TryGetValue(withoutFragment, out path!)) return true;
        var withoutQuery = new UriBuilder(url) { Query = string.Empty, Fragment = string.Empty }.Uri.ToString();
        return resourceObjectPaths.TryGetValue(withoutQuery, out path!);
    }
}
