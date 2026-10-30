namespace Archive.Core.Storage;

/// <summary>Applies version-independent security invariants at the snapshot content boundary.</summary>
internal static class SnapshotManifestValidation
{
    public static void Validate(ResolvedSnapshotManifest manifest, string contentPrefix)
    {
        if (manifest.Resources.Count == 0)
            throw new InvalidDataException("The snapshot manifest contains no browser-readable resources.");
        if (manifest.Resources.Count(resource => resource.Path == "index.html" &&
                resource.Role == SnapshotResourceRoles.RootDocument) != 1)
            throw new InvalidDataException("The snapshot manifest must define exactly one root document.");
        if (manifest.Resources.Select(resource => resource.Path).Distinct(StringComparer.Ordinal).Count() !=
            manifest.Resources.Count)
            throw new InvalidDataException("The snapshot manifest contains duplicate resource paths.");

        foreach (var resource in manifest.Resources)
        {
            if (string.IsNullOrWhiteSpace(resource.Path) || string.IsNullOrWhiteSpace(resource.ContentType) ||
                string.IsNullOrWhiteSpace(resource.ObjectKey))
                throw new InvalidDataException("The snapshot manifest contains an incomplete resource.");

            string normalized;
            try { normalized = SnapshotResourcePath.Normalize(resource.Path); }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException($"The snapshot manifest contains an unsafe path: {resource.Path}",
                    exception);
            }
            if (!string.Equals(normalized, resource.Path, StringComparison.Ordinal) ||
                !string.Equals(resource.ObjectKey, $"{contentPrefix}/{resource.Path}", StringComparison.Ordinal))
                throw new InvalidDataException(
                    $"The snapshot manifest maps a resource outside its generation: {resource.Path}");
            if (IsReserved(resource.Path))
                throw new InvalidDataException($"The snapshot manifest authorizes a reserved object: {resource.Path}");
            if (resource.Role is not (SnapshotResourceRoles.RootDocument or
                    SnapshotResourceRoles.NestedDocument or SnapshotResourceRoles.Asset))
                throw new InvalidDataException(
                    $"The snapshot manifest contains an unknown resource role: {resource.Role}");
            if (resource.Role is SnapshotResourceRoles.RootDocument or SnapshotResourceRoles.NestedDocument &&
                !IsHtml(resource.ContentType))
                throw new InvalidDataException(
                    $"The snapshot manifest assigns a document role to a non-HTML resource: {resource.Path}");
            if (resource.Sha256 is not null && !IsSha256(resource.Sha256))
                throw new InvalidDataException($"The snapshot manifest contains an invalid SHA-256: {resource.Path}");
        }
    }

    public static void ValidateProvenance(SnapshotProvenance provenance,
        IReadOnlyList<SnapshotResource> resources, SnapshotManifestReadContext context, int version)
    {
        if (string.IsNullOrWhiteSpace(provenance.SourceObjectKey) ||
            !TryGetRelativePath(context.ContentPrefix, provenance.SourceObjectKey, out var sourcePath) ||
            !IsSource(sourcePath) || string.IsNullOrWhiteSpace(provenance.SourceMimeType) ||
            string.IsNullOrWhiteSpace(provenance.DocumentState) ||
            string.IsNullOrWhiteSpace(provenance.SourceSha256) ||
            string.IsNullOrWhiteSpace(provenance.IndexSha256) ||
            !IsSha256(provenance.SourceSha256) || !IsSha256(provenance.IndexSha256))
            throw new InvalidDataException($"Snapshot manifest version {version} contains invalid provenance.");

        var roots = resources.Where(resource => resource is not null && resource.Path == "index.html" &&
            resource.Role == SnapshotResourceRoles.RootDocument).ToArray();
        if (roots.Length == 1 &&
            !string.Equals(roots[0].Sha256, provenance.IndexSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(
                $"Snapshot manifest version {version} has an inconsistent root-document hash.");
    }

    public static bool TryGetRelativePath(string contentPrefix, string objectKey, out string path)
    {
        var start = contentPrefix + "/";
        path = objectKey.StartsWith(start, StringComparison.Ordinal) ? objectKey[start.Length..] : string.Empty;
        if (string.IsNullOrWhiteSpace(path)) return false;
        try { return string.Equals(SnapshotResourcePath.Normalize(path), path, StringComparison.Ordinal); }
        catch (ArgumentException) { return false; }
    }

    public static bool IsReserved(string path) =>
        path is "manifest.json" or "screenshot.png" || IsSource(path);

    private static bool IsHtml(string contentType) =>
        contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);

    private static bool IsSource(string path) =>
        !path.Contains('/') && path.StartsWith("source.", StringComparison.OrdinalIgnoreCase);

    private static bool IsSha256(string value) => value.Length == 64 && value.All(character =>
        character is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');
}
