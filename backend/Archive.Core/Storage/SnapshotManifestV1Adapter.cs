namespace Archive.Core.Storage;

/// <summary>Adapts the V1 URL-capture manifest contract.</summary>
internal sealed class SnapshotManifestV1Adapter : LegacySnapshotManifestAdapter
{
    public SnapshotManifestV1Adapter() : base(1) { }

    protected override string? ResolveSourceUrl(LegacySnapshotManifestContract contract,
        SnapshotManifestReadContext context) => context.FallbackSourceUrl;
}
