using System.Text.Json.Serialization;
using Archive.Core.Storage.Manifests.Contracts.Shared;

namespace Archive.Core.Storage.Manifests.Contracts.V4;

/// <summary>Current versioned snapshot contract used to authorize stored resources at the public content boundary.</summary>
public sealed class SnapshotManifest
{
    public const int CurrentVersion = 4;

    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("provenance")]
    public required SnapshotProvenance Provenance { get; init; }

    [JsonPropertyName("interaction")]
    public required SnapshotInteractionMetadata Interaction { get; init; }

    [JsonPropertyName("resources")]
    public required IReadOnlyList<SnapshotResource> Resources { get; init; }

    [JsonPropertyName("missingUrls")]
    public IReadOnlyList<string> MissingUrls { get; init; } = [];

    [JsonPropertyName("missingUrlCount")]
    public int MissingUrlCount { get; init; }

    [JsonPropertyName("incomplete")]
    public bool Incomplete { get; init; }
}
