using System.Text.Json.Serialization;
using Archive.Core.Storage.Manifests.Contracts.Shared;

namespace Archive.Core.Storage.Manifests.Contracts.V3;

/// <summary>Persisted V3 contract whose interaction metadata was stored under <c>replay</c>.</summary>
internal sealed class SnapshotManifestV3Contract
{
    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("provenance")]
    public SnapshotProvenance? Provenance { get; init; }

    [JsonPropertyName("replay")]
    public SnapshotReplayMetadataV3Contract? Replay { get; init; }

    [JsonPropertyName("resources")]
    public IReadOnlyList<SnapshotResource>? Resources { get; init; }
}
