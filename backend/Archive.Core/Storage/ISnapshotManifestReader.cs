namespace Archive.Core.Storage;

/// <summary>Resolves a persisted, versioned snapshot manifest for the content-delivery boundary.</summary>
internal interface ISnapshotManifestReader
{
    Task<ResolvedSnapshotManifest> ReadAsync(Stream content, string contentPrefix,
        string? fallbackSourceUrl, CancellationToken cancellationToken);
}
