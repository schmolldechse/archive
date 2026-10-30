using System.Text.Json;
using Archive.Core.Storage;
using Archive.Core.Storage.Manifests;
using Archive.Core.Storage.Manifests.Contracts.Shared;
using Archive.Core.Storage.Manifests.Models;

namespace Archive.Core.Storage.Manifests.Adapters;

/// <summary>Maps exactly one persisted snapshot manifest contract into the version-independent read model.</summary>
internal abstract class SnapshotManifestAdapter
{
    protected SnapshotManifestAdapter(int version)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        Version = version;
    }

    public int Version { get; }

    public abstract ResolvedSnapshotManifest Resolve(JsonElement root, SnapshotManifestReadContext context);

    protected InvalidDataException IncompleteContract() =>
        new($"Snapshot manifest version {Version} is incomplete.");

    protected static ResolvedSnapshotInteraction ResolveInteraction(string normalizer, string? diagnostic)
    {
        var requiresNormalization =
            !string.Equals(normalizer, SnapshotDocumentNormalizer.Version, StringComparison.Ordinal) ||
            diagnostic is not null;
        return new(normalizer, diagnostic, requiresNormalization,
            SnapshotDocumentNormalizer.SupportsInputVersion(normalizer));
    }

    protected static IReadOnlyList<ResolvedSnapshotResource> ResolveResources(
        IEnumerable<SnapshotResource> resources) => resources.Select(resource => new ResolvedSnapshotResource(
            resource.Path, resource.SourceUrl, resource.ContentType, resource.ObjectKey, resource.Sha256,
            resource.Role)).ToArray();
}
