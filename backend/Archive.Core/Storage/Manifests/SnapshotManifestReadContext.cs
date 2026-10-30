namespace Archive.Core.Storage.Manifests;

/// <summary>Immutable contextual data supplied to every manifest-version adapter.</summary>
internal sealed record SnapshotManifestReadContext(string ContentPrefix, string? FallbackSourceUrl);
