using System.Text.Json;
using System.Text.Json.Serialization;

namespace Archive.Core.Storage;

/// <summary>Reads every supported persisted manifest version into the current content-delivery model.</summary>
internal static class SnapshotManifestReader
{
    public static async Task<ResolvedSnapshotManifest> ReadAsync(Stream content, string contentPrefix,
        string? fallbackSourceUrl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentPrefix);

        using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("version", out var versionElement) ||
            !versionElement.TryGetInt32(out var version))
            throw new InvalidDataException("The snapshot manifest has no integer version.");

        var prefix = contentPrefix.TrimEnd('/');
        var resolved = version switch
        {
            1 or 2 => ResolveLegacy(root, version, prefix, fallbackSourceUrl),
            3 => ResolveVersion3(Deserialize<SnapshotManifestV3>(root, version), prefix),
            SnapshotManifest.CurrentVersion => ResolveVersion4(Deserialize<SnapshotManifest>(root, version), prefix),
            _ => throw new InvalidDataException($"Snapshot manifest version {version} is not supported.")
        };

        Validate(resolved, prefix);
        return resolved;
    }

    private static T Deserialize<T>(JsonElement root, int version) where T : class =>
        root.Deserialize<T>() ??
        throw new InvalidDataException($"Snapshot manifest version {version} could not be deserialized.");

    private static ResolvedSnapshotManifest ResolveVersion4(SnapshotManifest manifest, string prefix)
    {
        if (manifest.Version != SnapshotManifest.CurrentVersion || manifest.Provenance is null ||
            manifest.Interaction is null || manifest.Resources is null ||
            manifest.Resources.Any(resource => resource is null) ||
            string.IsNullOrWhiteSpace(manifest.Interaction.Normalizer))
            throw new InvalidDataException("Snapshot manifest version 4 is incomplete.");

        ValidateProvenance(manifest.Provenance, manifest.Resources, prefix, manifest.Version);
        var requiresNormalization =
            !string.Equals(manifest.Interaction.Normalizer, SnapshotDocumentNormalizer.Version,
                StringComparison.Ordinal) || manifest.Interaction.Diagnostic is not null;
        return new(
            manifest.Version,
            new(manifest.Provenance.SourceUrl),
            new(manifest.Interaction.Normalizer, manifest.Interaction.Diagnostic, requiresNormalization,
                SnapshotDocumentNormalizer.SupportsInputVersion(manifest.Interaction.Normalizer)),
            manifest.Resources.Select(resource => new ResolvedSnapshotResource(
                resource.Path, resource.SourceUrl, resource.ContentType, resource.ObjectKey, resource.Sha256,
                resource.Role)).ToArray());
    }

    private static ResolvedSnapshotManifest ResolveVersion3(SnapshotManifestV3 manifest, string prefix)
    {
        if (manifest.Version != 3 || manifest.Provenance is null || manifest.Replay is null ||
            manifest.Resources is null || manifest.Resources.Any(resource => resource is null) ||
            string.IsNullOrWhiteSpace(manifest.Replay.Normalizer))
            throw new InvalidDataException("Snapshot manifest version 3 is incomplete.");

        ValidateProvenance(manifest.Provenance, manifest.Resources, prefix, manifest.Version);
        var requiresNormalization =
            !string.Equals(manifest.Replay.Normalizer, SnapshotDocumentNormalizer.Version,
                StringComparison.Ordinal) || manifest.Replay.Diagnostic is not null;
        return new(
            manifest.Version,
            new(manifest.Provenance.SourceUrl),
            new(manifest.Replay.Normalizer, manifest.Replay.Diagnostic, requiresNormalization,
                SnapshotDocumentNormalizer.SupportsInputVersion(manifest.Replay.Normalizer)),
            manifest.Resources.Select(resource => new ResolvedSnapshotResource(
                resource.Path, resource.SourceUrl, resource.ContentType, resource.ObjectKey, resource.Sha256,
                resource.Role)).ToArray());
    }

    private static ResolvedSnapshotManifest ResolveLegacy(JsonElement root, int version, string prefix,
        string? fallbackSourceUrl)
    {
        var manifest = Deserialize<LegacySnapshotManifest>(root, version);
        if (manifest.Version != version || string.IsNullOrWhiteSpace(manifest.HtmlObjectKey) ||
            manifest.Resources is null)
            throw new InvalidDataException($"Snapshot manifest version {version} is incomplete.");

        var sourceUrl = version == 2 && !string.IsNullOrWhiteSpace(manifest.OriginalUrl)
            ? manifest.OriginalUrl : fallbackSourceUrl;
        var resources = new List<ResolvedSnapshotResource>(manifest.Resources.Count + 1)
        {
            new("index.html", sourceUrl, "text/html; charset=utf-8", manifest.HtmlObjectKey, null,
                SnapshotResourceRoles.RootDocument)
        };

        foreach (var resource in manifest.Resources)
        {
            if (string.IsNullOrWhiteSpace(resource.ObjectKey) ||
                !TryRelativePath(prefix, resource.ObjectKey, out var path) ||
                string.IsNullOrWhiteSpace(resource.ContentType))
                throw new InvalidDataException($"Snapshot manifest version {version} contains an invalid resource.");
            if (IsReserved(path))
                throw new InvalidDataException($"Snapshot manifest version {version} authorizes a reserved object.");

            resources.Add(new(path, resource.Url, resource.ContentType, resource.ObjectKey, null,
                SnapshotResourceRoles.Infer(path, resource.ContentType)));
        }

        return new(version, new(sourceUrl), new(null, "Legacy manifest requires current normalization.", true, true),
            resources);
    }

    private static void Validate(ResolvedSnapshotManifest manifest, string prefix)
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
                !string.Equals(resource.ObjectKey, $"{prefix}/{resource.Path}", StringComparison.Ordinal))
                throw new InvalidDataException($"The snapshot manifest maps a resource outside its generation: {resource.Path}");
            if (IsReserved(resource.Path))
                throw new InvalidDataException($"The snapshot manifest authorizes a reserved object: {resource.Path}");
            if (resource.Role is not (SnapshotResourceRoles.RootDocument or
                    SnapshotResourceRoles.NestedDocument or SnapshotResourceRoles.Asset))
                throw new InvalidDataException($"The snapshot manifest contains an unknown resource role: {resource.Role}");
            if (resource.Role is SnapshotResourceRoles.RootDocument or SnapshotResourceRoles.NestedDocument &&
                !IsHtml(resource.ContentType))
                throw new InvalidDataException($"The snapshot manifest assigns a document role to a non-HTML resource: {resource.Path}");
            if (resource.Sha256 is not null && !IsSha256(resource.Sha256))
                throw new InvalidDataException($"The snapshot manifest contains an invalid SHA-256: {resource.Path}");
        }
    }

    private static void ValidateProvenance(SnapshotProvenance provenance,
        IReadOnlyList<SnapshotResource> resources, string prefix, int version)
    {
        if (string.IsNullOrWhiteSpace(provenance.SourceObjectKey) ||
            !TryRelativePath(prefix, provenance.SourceObjectKey, out var sourcePath) || !IsSource(sourcePath) ||
            string.IsNullOrWhiteSpace(provenance.SourceMimeType) ||
            string.IsNullOrWhiteSpace(provenance.DocumentState) ||
            string.IsNullOrWhiteSpace(provenance.SourceSha256) ||
            string.IsNullOrWhiteSpace(provenance.IndexSha256) ||
            !IsSha256(provenance.SourceSha256) || !IsSha256(provenance.IndexSha256))
            throw new InvalidDataException($"Snapshot manifest version {version} contains invalid provenance.");

        var roots = resources.Where(resource => resource is not null && resource.Path == "index.html" &&
            resource.Role == SnapshotResourceRoles.RootDocument).ToArray();
        if (roots.Length == 1 &&
            !string.Equals(roots[0].Sha256, provenance.IndexSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Snapshot manifest version {version} has an inconsistent root-document hash.");
    }

    private static bool TryRelativePath(string prefix, string objectKey, out string path)
    {
        var start = prefix + "/";
        path = objectKey.StartsWith(start, StringComparison.Ordinal) ? objectKey[start.Length..] : string.Empty;
        if (string.IsNullOrWhiteSpace(path)) return false;
        try { return string.Equals(SnapshotResourcePath.Normalize(path), path, StringComparison.Ordinal); }
        catch (ArgumentException) { return false; }
    }

    private static bool IsHtml(string contentType) =>
        contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);

    private static bool IsSource(string path) =>
        !path.Contains('/') && path.StartsWith("source.", StringComparison.OrdinalIgnoreCase);

    private static bool IsReserved(string path) =>
        path is "manifest.json" or "screenshot.png" || IsSource(path);

    private static bool IsSha256(string value) => value.Length == 64 && value.All(character =>
        character is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');

    private sealed class LegacySnapshotManifest
    {
        [JsonPropertyName("version")]
        public int Version { get; init; }

        [JsonPropertyName("htmlObjectKey")]
        public string? HtmlObjectKey { get; init; }

        [JsonPropertyName("originalUrl")]
        public string? OriginalUrl { get; init; }

        [JsonPropertyName("resources")]
        public IReadOnlyList<LegacySnapshotResource>? Resources { get; init; }
    }

    private sealed class LegacySnapshotResource
    {
        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("contentType")]
        public string? ContentType { get; init; }

        [JsonPropertyName("objectKey")]
        public string? ObjectKey { get; init; }
    }

    private sealed class SnapshotManifestV3
    {
        [JsonPropertyName("version")]
        public int Version { get; init; }

        [JsonPropertyName("provenance")]
        public SnapshotProvenance? Provenance { get; init; }

        [JsonPropertyName("replay")]
        public SnapshotReplayMetadataV3? Replay { get; init; }

        [JsonPropertyName("resources")]
        public IReadOnlyList<SnapshotResource>? Resources { get; init; }
    }

    private sealed class SnapshotReplayMetadataV3
    {
        [JsonPropertyName("normalizer")]
        public string? Normalizer { get; init; }

        [JsonPropertyName("diagnostic")]
        public string? Diagnostic { get; init; }
    }
}
