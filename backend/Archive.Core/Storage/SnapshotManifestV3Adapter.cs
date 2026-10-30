namespace Archive.Core.Storage;

/// <summary>Adapts the historical V3 replay metadata to the current interaction read model.</summary>
internal sealed class SnapshotManifestV3Adapter : SnapshotManifestAdapter<SnapshotManifestV3Contract>
{
    public SnapshotManifestV3Adapter() : base(3) { }

    protected override ResolvedSnapshotManifest Resolve(SnapshotManifestV3Contract contract,
        SnapshotManifestReadContext context)
    {
        if (contract.Version != Version || contract.Provenance is null || contract.Replay is null ||
            contract.Resources is null || contract.Resources.Any(resource => resource is null) ||
            string.IsNullOrWhiteSpace(contract.Replay.Normalizer))
            throw IncompleteContract();

        SnapshotManifestValidation.ValidateProvenance(
            contract.Provenance, contract.Resources, context, Version);
        return new(
            Version,
            new(contract.Provenance.SourceUrl),
            ResolveInteraction(contract.Replay.Normalizer, contract.Replay.Diagnostic),
            ResolveResources(contract.Resources));
    }
}
