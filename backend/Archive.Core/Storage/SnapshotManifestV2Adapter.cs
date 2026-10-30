namespace Archive.Core.Storage;

/// <summary>Adapts the V2 imported-document manifest contract.</summary>
internal sealed class SnapshotManifestV2Adapter : LegacySnapshotManifestAdapter
{
    public SnapshotManifestV2Adapter() : base(2) { }

    protected override string? ResolveSourceUrl(LegacySnapshotManifestContract contract,
        SnapshotManifestReadContext context) =>
        !string.IsNullOrWhiteSpace(contract.OriginalUrl) ? contract.OriginalUrl : context.FallbackSourceUrl;
}
