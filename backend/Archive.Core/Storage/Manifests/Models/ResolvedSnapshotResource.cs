namespace Archive.Core.Storage.Manifests.Models;

/// <summary>One validated browser-readable object from any supported manifest version.</summary>
internal sealed record ResolvedSnapshotResource(
    string Path,
    string? SourceUrl,
    string ContentType,
    string ObjectKey,
    string? Sha256,
    string Role);
