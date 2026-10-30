namespace Archive.Core.Storage;

/// <summary>Validated, version-independent snapshot manifest used by the content read boundary.</summary>
internal sealed record ResolvedSnapshotManifest(
    int Version,
    ResolvedSnapshotProvenance Provenance,
    ResolvedSnapshotInteraction Interaction,
    IReadOnlyList<ResolvedSnapshotResource> Resources);

/// <summary>Provenance fields needed while upgrading stored documents for current replay.</summary>
internal sealed record ResolvedSnapshotProvenance(string? SourceUrl);

/// <summary>Describes whether a stored document must and can be normalized before delivery.</summary>
internal sealed record ResolvedSnapshotInteraction(
    string? Normalizer,
    string? Diagnostic,
    bool RequiresNormalization,
    bool CanNormalize);

/// <summary>One validated browser-readable object from any supported manifest version.</summary>
internal sealed record ResolvedSnapshotResource(
    string Path,
    string? SourceUrl,
    string ContentType,
    string ObjectKey,
    string? Sha256,
    string Role);
