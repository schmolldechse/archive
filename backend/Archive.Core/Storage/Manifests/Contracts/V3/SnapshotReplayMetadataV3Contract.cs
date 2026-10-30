using System.Text.Json.Serialization;

namespace Archive.Core.Storage.Manifests.Contracts.V3;

/// <summary>Interaction metadata persisted by the V3 replay contract.</summary>
internal sealed class SnapshotReplayMetadataV3Contract
{
    [JsonPropertyName("normalizer")]
    public string? Normalizer { get; init; }

    [JsonPropertyName("diagnostic")]
    public string? Diagnostic { get; init; }
}
