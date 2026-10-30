using System.Text.Json.Serialization;

namespace Archive.Core.Storage.Manifests.Contracts.Legacy;

/// <summary>Resource entry persisted identically by the V1 and V2 manifest contracts.</summary>
internal sealed class LegacySnapshotResourceContract
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; init; }

    [JsonPropertyName("objectKey")]
    public string? ObjectKey { get; init; }
}
