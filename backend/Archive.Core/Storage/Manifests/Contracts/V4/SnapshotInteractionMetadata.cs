using System.Text.Json.Serialization;

namespace Archive.Core.Storage.Manifests.Contracts.V4;

/// <summary>Interaction normalization diagnostics for the canonical entry document.</summary>
public sealed class SnapshotInteractionMetadata
{
    [JsonPropertyName("normalizer")]
    public required string Normalizer { get; init; }

    [JsonPropertyName("containsExecutableContent")]
    public bool ContainsExecutableContent { get; init; }

    [JsonPropertyName("diagnostic")]
    public string? Diagnostic { get; init; }
}
