using System.Text.Json;
using Archive.Core.Models.Snapshots;

namespace Archive.Core.Storage;

/// <summary>Reads and validates metadata from an active snapshot generation.</summary>
public sealed class SnapshotManifestReader(IArchiveObjectStore objectStore)
{
    public const int MaxPageTreePages = 10_001;
    public const int MaxPageTitleLength = 512;

    /// <summary>Reads a current manifest, or returns null when it is missing, malformed, or unsupported.</summary>
    public async Task<SnapshotManifest?> ReadAsync(string contentPrefix,
        CancellationToken cancellationToken = default)
    {
        var stored = await objectStore.GetAsync($"{contentPrefix}/manifest.json", cancellationToken);
        if (stored is null)
            return null;

        try
        {
            await using (stored.Content)
            {
                var manifest = await JsonSerializer.DeserializeAsync<SnapshotManifest>(stored.Content,
                    cancellationToken: cancellationToken);
                return manifest?.Version == SnapshotManifest.CurrentVersion ? manifest : null;
            }
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns a page tree only when every node is a manifest-authorized document in one acyclic hierarchy.
    /// </summary>
    public static bool TryGetPageTree(SnapshotManifest manifest, out SnapshotPageTree pageTree)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        pageTree = null!;
        var candidate = manifest.PageTree;
        if (manifest.Version != SnapshotManifest.CurrentVersion || candidate is null ||
            candidate.Pages.Count is < 1 or > MaxPageTreePages ||
            !TryNormalizePath(candidate.RootPath, out var rootPath) || rootPath != candidate.RootPath)
            return false;

        var documentResources = new Dictionary<string, SnapshotResource>(StringComparer.Ordinal);
        foreach (var resource in manifest.Resources)
        {
            if (resource.Role is not (SnapshotResourceRoles.RootDocument or SnapshotResourceRoles.NestedDocument))
                continue;
            if (!TryNormalizePath(resource.Path, out var resourcePath) || resourcePath != resource.Path ||
                !documentResources.TryAdd(resourcePath, resource))
                return false;
        }
        if (documentResources.Count != candidate.Pages.Count ||
            documentResources.Values.Count(resource => resource.Role == SnapshotResourceRoles.RootDocument) != 1)
            return false;

        var pages = new Dictionary<string, SnapshotPageTreeEntry>(StringComparer.Ordinal);
        foreach (var page in candidate.Pages)
        {
            if (!TryNormalizePath(page.Path, out var path) || path != page.Path ||
                page.ParentPath is not null && (!TryNormalizePath(page.ParentPath, out var parentPath) ||
                    parentPath != page.ParentPath) ||
                !Uri.TryCreate(page.SourceUrl, UriKind.Absolute, out var sourceUrl) ||
                sourceUrl.Scheme is not ("http" or "https") ||
                page.SourceUrl.Length > SnapshotValidation.MaxSourceUrlLength ||
                !IsNormalizedTitle(page.Title) ||
                !documentResources.TryGetValue(path, out var resource) ||
                resource.Role != (path == rootPath
                    ? SnapshotResourceRoles.RootDocument
                    : SnapshotResourceRoles.NestedDocument) ||
                !string.Equals(resource.SourceUrl, page.SourceUrl, StringComparison.Ordinal) ||
                !pages.TryAdd(path, page))
                return false;
        }

        if (!pages.TryGetValue(rootPath, out var root) || root.ParentPath is not null ||
            documentResources[rootPath].Role != SnapshotResourceRoles.RootDocument ||
            pages.Values.Count(page => page.ParentPath is null) != 1)
            return false;

        var children = pages.Keys.ToDictionary(path => path, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var page in pages.Values)
        {
            if (page.ParentPath is null)
                continue;
            if (!pages.ContainsKey(page.ParentPath))
                return false;
            children[page.ParentPath].Add(page.Path);
        }

        var reached = new HashSet<string>(StringComparer.Ordinal);
        var breadthFirstPaths = new List<string>(pages.Count);
        var pending = new Queue<string>();
        pending.Enqueue(rootPath);
        while (pending.TryDequeue(out var path))
        {
            if (!reached.Add(path))
                return false;
            breadthFirstPaths.Add(path);
            foreach (var child in children[path])
                pending.Enqueue(child);
        }

        if (reached.Count != pages.Count ||
            !breadthFirstPaths.SequenceEqual(candidate.Pages.Select(page => page.Path), StringComparer.Ordinal))
            return false;

        pageTree = candidate;
        return true;
    }

    private static bool TryNormalizePath(string? value, out string normalized)
    {
        try
        {
            normalized = SnapshotResourcePath.Normalize(value!);
            return true;
        }
        catch (ArgumentException)
        {
            normalized = string.Empty;
            return false;
        }
    }

    private static bool IsNormalizedTitle(string? title)
    {
        if (title is null)
            return true;
        if (title.Length is < 1 or > MaxPageTitleLength || title[0] == ' ' || title[^1] == ' ')
            return false;

        var previousWasSpace = false;
        foreach (var character in title)
        {
            if (char.IsControl(character) || char.IsWhiteSpace(character) && character != ' ' ||
                character == ' ' && previousWasSpace)
                return false;
            previousWasSpace = character == ' ';
        }

        return true;
    }
}
