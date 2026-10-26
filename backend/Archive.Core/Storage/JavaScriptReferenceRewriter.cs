using System.Net;
using System.Text.RegularExpressions;

namespace Archive.Core.Storage;

/// <summary>Rewrites bounded literal URL forms to captured snapshot resources.</summary>
public static partial class JavaScriptReferenceRewriter
{
    /// <summary>Rewrites captured literal URL forms relative to the stored JavaScript object.</summary>
    public static string Rewrite(string source, string? sourceUrl, IReadOnlyDictionary<string, string> map)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var baseUri))
            return source;

        source = StaticModule().Replace(source, match => Replace(match, baseUri, map));
        source = DynamicImport().Replace(source, match => Replace(match, baseUri, map));
        source = ImportMetaUrl().Replace(source, match => Replace(match, baseUri, map));
        source = LiteralRuntimeRead().Replace(source, match => Replace(match, baseUri, map));
        return source;
    }

    private static string Replace(Match match, Uri baseUri, IReadOnlyDictionary<string, string> map)
    {
        var raw = WebUtility.HtmlDecode(match.Groups["url"].Value);
        if (!Uri.TryCreate(baseUri, raw, out var absolute))
            return match.Value;
        if (!map.TryGetValue(SnapshotResourcePath.ToAbsoluteUrlKey(absolute), out var local))
            return match.Value;
        return match.Groups["prefix"].Value + match.Groups["quote"].Value + AsRelativeUrl(local) +
            match.Groups["quote"].Value + match.Groups["suffix"].Value;
    }

    /// <summary>Keeps a captured path URL-like when it is used as a module specifier.</summary>
    internal static string AsRelativeUrl(string value)
    {
        // Bare specifiers are package names in an import declaration. Prefixing the
        // first path segment keeps the same value valid for modules and URL APIs.
        if (value.StartsWith("./", StringComparison.Ordinal) ||
            value.StartsWith("../", StringComparison.Ordinal) ||
            value.StartsWith("/", StringComparison.Ordinal) ||
            Uri.TryCreate(value, UriKind.Absolute, out _))
            return value;
        return "./" + value;
    }

    [GeneratedRegex("(?<prefix>\\b(?:import|export)\\s+(?:(?:[^;\\r\\n]*?)\\s+from\\s+)?)(?<quote>['\"])(?<url>[^'\"]+)\\k<quote>(?<suffix>)",
        RegexOptions.CultureInvariant)]
    private static partial Regex StaticModule();

    [GeneratedRegex("(?<prefix>\\bimport\\s*\\(\\s*)(?<quote>['\"])(?<url>[^'\"]+)\\k<quote>(?<suffix>\\s*\\))",
        RegexOptions.CultureInvariant)]
    private static partial Regex DynamicImport();

    [GeneratedRegex("(?<prefix>\\bnew\\s+URL\\s*\\(\\s*)(?<quote>['\"])(?<url>[^'\"]+)\\k<quote>(?<suffix>\\s*,\\s*import\\.meta\\.url\\s*\\))",
        RegexOptions.CultureInvariant)]
    private static partial Regex ImportMetaUrl();

    [GeneratedRegex("(?<prefix>\\b(?:fetch|Request)\\s*\\(\\s*)(?<quote>['\"])(?<url>[^'\"]+)\\k<quote>(?<suffix>)",
        RegexOptions.CultureInvariant)]
    private static partial Regex LiteralRuntimeRead();
}
