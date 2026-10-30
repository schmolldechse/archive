namespace Archive.Core.Storage;

/// <summary>Adapts the current strict V4 writer contract to the content-delivery read model.</summary>
internal sealed class SnapshotManifestV4Adapter : SnapshotManifestAdapter<SnapshotManifest>
{
    public SnapshotManifestV4Adapter() : base(SnapshotManifest.CurrentVersion) { }

    protected override ResolvedSnapshotManifest Resolve(SnapshotManifest contract,
        SnapshotManifestReadContext context)
    {
        if (contract.Version != Version || contract.Provenance is null || contract.Interaction is null ||
            contract.Resources is null || contract.Resources.Any(resource => resource is null) ||
            string.IsNullOrWhiteSpace(contract.Interaction.Normalizer))
            throw IncompleteContract();

        SnapshotManifestValidation.ValidateProvenance(
            contract.Provenance, contract.Resources, context, Version);
        return new(
            Version,
            new(contract.Provenance.SourceUrl),
            ResolveInteraction(contract.Interaction.Normalizer, contract.Interaction.Diagnostic),
            ResolveResources(contract.Resources));
    }
}
