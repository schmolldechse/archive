using System.Net;
using System.Text.RegularExpressions;

namespace Archive.Core.Storage;

/// <summary>Builds route-neutral resource references for objects within one archived snapshot.</summary>
public static class SnapshotResourcePath
{
    /// <summary>Returns a relative HTML base that resolves from a document to the snapshot root.</summary>
    public static string HtmlBaseFor(string documentPath)
    {
        var segments = Normalize(documentPath).Split('/');
        var directoryDepth = segments.Length - 1;
        return directoryDepth == 0 ? "./" : string.Concat(Enumerable.Repeat("../", directoryDepth));
    }

    /// <summary>Resolves an HTML document's declared base URL before the tag is replaced for offline playback.</summary>
    public static string? ResolveDocumentBase(string html, string? fallbackUrl)
    {
        var match = Regex.Match(html,
            "<base\\b[^>]*\\bhref\\s*=\\s*(?<quote>[\\\"'])(?<value>.*?)\\k<quote>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(1));
        if (!match.Success)
            return fallbackUrl;

        var href = WebUtility.HtmlDecode(match.Groups["value"].Value).Trim();
        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute) && IsHttp(absolute))
            return absolute.ToString();

        if (Uri.TryCreate(fallbackUrl, UriKind.Absolute, out var fallback) &&
            Uri.TryCreate(fallback, href, out var resolved) && IsHttp(resolved))
            return resolved.ToString();

        return fallbackUrl;
    }

    /// <summary>Returns a URL path from the directory of one snapshot object to another snapshot object.</summary>
    public static string RelativeTo(string sourceDocumentPath, string targetObjectPath)
    {
        var source = Normalize(sourceDocumentPath).Split('/');
        var target = Normalize(targetObjectPath).Split('/');
        var sourceDirectory = source[..^1];
        var shared = 0;
        while (shared < sourceDirectory.Length && shared < target.Length &&
               string.Equals(sourceDirectory[shared], target[shared], StringComparison.Ordinal))
            shared++;

        var parents = Enumerable.Repeat("..", sourceDirectory.Length - shared);
        var remainder = target[shared..];
        var result = string.Join('/', parents.Concat(remainder));
        return result.Length == 0 ? "./" : result;
    }

    /// <summary>Validates and normalizes a path relative to the snapshot content root.</summary>
    public static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var normalized = path.Replace('\\', '/');
        var segments = normalized.Split('/');
        if (normalized.Length == 0 || normalized.StartsWith('/') || segments.Any(segment => segment is "" or "." or ".."))
            throw new ArgumentException("Snapshot object paths must be safe relative paths.", nameof(path));
        return normalized;
    }

    private static bool IsHttp(Uri uri) =>
        uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
        uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
