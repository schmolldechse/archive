using System.Text;
using Archive.Core.Storage;

namespace Archive.SnapshotMigrator;

/// <summary>Describes one display resource from an existing snapshot generation.</summary>
internal sealed record SnapshotMigrationResourceDescriptor(
    string Path,
    string? SourceUrl,
    string ContentType);

/// <summary>Contains a resource after applying the current canonical snapshot normalization.</summary>
internal sealed record NormalizedSnapshotMigrationResource(
    byte[] Body,
    string ContentType,
    string Role,
    bool ContainsExecutableContent);

/// <summary>Applies the worker's canonical snapshot rules to resources from an older generation.</summary>
internal static class SnapshotMigrationNormalizer
{
    public static IReadOnlyDictionary<string, string> BuildSourceMap(
        IEnumerable<SnapshotMigrationResourceDescriptor> resources)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var resource in resources)
        {
            if (resource.Path == "index.html" || string.IsNullOrWhiteSpace(resource.SourceUrl))
                continue;
            var sourceUrl = WithoutFragment(resource.SourceUrl);
            result.TryAdd(sourceUrl, resource.Path);
        }
        return result;
    }

    public static NormalizedSnapshotMigrationResource Normalize(
        SnapshotMigrationResourceDescriptor resource,
        byte[] body,
        IReadOnlyDictionary<string, string> sourceMap)
    {
        var relativeMap = sourceMap.ToDictionary(
            pair => pair.Key,
            pair => SnapshotResourcePath.RelativeTo(resource.Path, pair.Value),
            StringComparer.Ordinal);

        if (IsHtml(resource.ContentType))
        {
            var html = SnapshotTextEncoding.Decode(body, resource.ContentType, out _);
            var document = SnapshotDocumentNormalizer.Normalize(
                html, resource.SourceUrl, relativeMap, resource.Path);
            return new(
                Encoding.UTF8.GetBytes(document.Html),
                "text/html; charset=utf-8",
                resource.Path == "index.html"
                    ? SnapshotResourceRoles.RootDocument
                    : SnapshotResourceRoles.NestedDocument,
                document.ContainsExecutableContent);
        }

        if (IsJavaScript(resource.ContentType))
        {
            var script = SnapshotTextEncoding.Decode(body, resource.ContentType, out _);
            var rewritten = JavaScriptReferenceRewriter.Rewrite(script, resource.SourceUrl, relativeMap);
            return new(
                Encoding.UTF8.GetBytes(rewritten),
                "text/javascript; charset=utf-8",
                SnapshotResourceRoles.Asset,
                true);
        }

        return new(body, resource.ContentType,
            SnapshotResourceRoles.Infer(resource.Path, resource.ContentType), false);
    }

    private static bool IsHtml(string contentType) =>
        contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);

    private static bool IsJavaScript(string contentType) =>
        contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
        contentType.Contains("ecmascript", StringComparison.OrdinalIgnoreCase);

    private static string WithoutFragment(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Fragment))
            return value;
        return new UriBuilder(uri) { Fragment = string.Empty }.Uri.ToString();
    }
}
