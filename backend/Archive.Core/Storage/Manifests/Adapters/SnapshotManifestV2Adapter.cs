using Archive.Core.Storage.Manifests;
using Archive.Core.Storage.Manifests.Contracts.V2;

namespace Archive.Core.Storage.Manifests.Adapters;

/// <summary>Adapts the V2 imported-document manifest contract.</summary>
internal sealed class SnapshotManifestV2Adapter : LegacySnapshotManifestAdapter<SnapshotManifestV2Contract>
{
    public SnapshotManifestV2Adapter() : base(2) { }

    protected override string? ResolveSourceUrl(SnapshotManifestV2Contract contract,
        SnapshotManifestReadContext context) =>
        !string.IsNullOrWhiteSpace(contract.OriginalUrl) ? contract.OriginalUrl : context.FallbackSourceUrl;
}
