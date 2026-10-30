using Archive.Core.Storage.Manifests;
using Archive.Core.Storage.Manifests.Contracts.V1;

namespace Archive.Core.Storage.Manifests.Adapters;

/// <summary>Adapts the V1 URL-capture manifest contract.</summary>
internal sealed class SnapshotManifestV1Adapter : LegacySnapshotManifestAdapter<SnapshotManifestV1Contract>
{
    public SnapshotManifestV1Adapter() : base(1) { }

    protected override string? ResolveSourceUrl(SnapshotManifestV1Contract contract,
        SnapshotManifestReadContext context) => context.FallbackSourceUrl;
}
