namespace Archive.Core.Storage.Manifests.Models;

/// <summary>Describes whether a stored document must and can be normalized before delivery.</summary>
internal sealed record ResolvedSnapshotInteraction(
    string? Normalizer,
    string? Diagnostic,
    bool RequiresNormalization,
    bool CanNormalize);
