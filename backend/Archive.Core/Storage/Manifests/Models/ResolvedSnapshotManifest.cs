namespace Archive.Core.Storage.Manifests.Models;

/// <summary>Validated, version-independent snapshot manifest used by the content read boundary.</summary>
internal sealed record ResolvedSnapshotManifest(
    int Version,
    ResolvedSnapshotProvenance Provenance,
    ResolvedSnapshotInteraction Interaction,
    IReadOnlyList<ResolvedSnapshotResource> Resources);
