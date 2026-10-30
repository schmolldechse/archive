using System.Text.Json.Serialization;

namespace Archive.Core.Storage.Manifests.Contracts.Shared;

/// <summary>A manifest-authorized object and the browser role it may fulfill.</summary>
public sealed class SnapshotResource
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("sourceUrl")]
    public string? SourceUrl { get; init; }

    [JsonPropertyName("contentType")]
    public required string ContentType { get; init; }

    [JsonPropertyName("objectKey")]
    public required string ObjectKey { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }
}
