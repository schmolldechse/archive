using System.Text.Json;
using System.Text.Json.Nodes;

namespace Archive.Core.Storage;

/// <summary>Maps captured import-map targets, including prefix mappings, to immutable snapshot resources.</summary>
public static class SnapshotImportMapRewriter
{
    /// <summary>Rewrites captured import-map targets and scopes to their snapshot-local paths.</summary>
    public static string Rewrite(string source, string? sourceUrl, IReadOnlyDictionary<string, string> resources)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var baseUri))
            return source;

        JsonObject? importMap;
        try
        {
            importMap = JsonNode.Parse(source) as JsonObject;
        }
        catch (JsonException)
        {
            // Invalid import maps already fail in the browser; preserve their source for provenance.
            return source;
        }

        if (importMap is null)
            return source;
        if (importMap["imports"] is JsonObject imports)
            RewriteSpecifierMap(imports, baseUri, resources);
        if (importMap["scopes"] is JsonObject scopes)
            RewriteScopes(scopes, baseUri, resources);

        return importMap.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static void RewriteScopes(JsonObject scopes, Uri baseUri,
        IReadOnlyDictionary<string, string> resources)
    {
        var rewritten = new JsonObject();
        foreach (var scope in scopes.ToArray())
        {
            if (scope.Value is not JsonObject specifiers ||
                !TryResolve(baseUri, scope.Key, out var absoluteScope))
            {
                rewritten[scope.Key] = scope.Value?.DeepClone();
                continue;
            }

            RewriteSpecifierMap(specifiers, baseUri, resources);
            var localScopes = resources
                .Where(resource => resource.Key.StartsWith(absoluteScope, StringComparison.Ordinal))
                .Select(resource => JavaScriptReferenceRewriter.AsRelativeUrl(resource.Value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (localScopes.Length == 0)
            {
                rewritten[scope.Key] = specifiers.DeepClone();
                continue;
            }

            // Captured objects use collision-proof names rather than their source directory. Give every local
            // module from the original scope the same mapping so scoped bare imports retain their semantics.
            foreach (var localScope in localScopes)
                rewritten[localScope] = specifiers.DeepClone();
        }

        scopes.Clear();
        foreach (var scope in rewritten)
            scopes[scope.Key] = scope.Value?.DeepClone();
    }

    private static void RewriteSpecifierMap(JsonObject specifiers, Uri baseUri,
        IReadOnlyDictionary<string, string> resources)
    {
        var entries = specifiers.ToArray();
        foreach (var entry in entries)
        {
            if (entry.Value is not JsonValue value || !value.TryGetValue<string>(out var target) ||
                !TryResolve(baseUri, target, out var absoluteTarget))
                continue;

            if (resources.TryGetValue(absoluteTarget, out var local))
                specifiers[entry.Key] = JavaScriptReferenceRewriter.AsRelativeUrl(local);

            if (!entry.Key.EndsWith("/", StringComparison.Ordinal) ||
                !absoluteTarget.EndsWith("/", StringComparison.Ordinal))
                continue;

            // A captured store uses collision-proof object names, so a URL-prefix target cannot remain a prefix.
            // Expand it into exact specifiers for every captured resource below the original target.
            foreach (var resource in resources)
            {
                if (!resource.Key.StartsWith(absoluteTarget, StringComparison.Ordinal))
                    continue;
                var exactSpecifier = entry.Key + resource.Key[absoluteTarget.Length..];
                if (!specifiers.ContainsKey(exactSpecifier))
                    specifiers[exactSpecifier] = JavaScriptReferenceRewriter.AsRelativeUrl(resource.Value);
            }
        }
    }

    private static bool TryResolve(Uri baseUri, string value, out string absolute)
    {
        absolute = string.Empty;
        if (!Uri.TryCreate(baseUri, value, out var resolved))
            return false;
        absolute = SnapshotResourcePath.ToAbsoluteUrlKey(resolved);
        return true;
    }
}
