namespace Archive.Core.Storage.Manifests.Contracts.Legacy;

/// <summary>Common read shape shared by the otherwise distinct V1 and V2 manifest contracts.</summary>
internal interface ILegacySnapshotManifestContract
{
    int Version { get; }
    string? HtmlObjectKey { get; }
    IReadOnlyList<LegacySnapshotResourceContract>? Resources { get; }
}
