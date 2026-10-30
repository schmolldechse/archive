using Archive.Core.Storage.Manifests.Models;

namespace Archive.Core.Storage.Manifests;

/// <summary>Resolves a persisted, versioned snapshot manifest for the content-delivery boundary.</summary>
internal interface ISnapshotManifestReader
{
    Task<ResolvedSnapshotManifest> ReadAsync(Stream content, string contentPrefix,
        string? fallbackSourceUrl, CancellationToken cancellationToken);
}
