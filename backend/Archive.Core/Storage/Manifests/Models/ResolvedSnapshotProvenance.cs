namespace Archive.Core.Storage.Manifests.Models;

/// <summary>Provenance fields needed while upgrading stored documents for current replay.</summary>
internal sealed record ResolvedSnapshotProvenance(string? SourceUrl);
