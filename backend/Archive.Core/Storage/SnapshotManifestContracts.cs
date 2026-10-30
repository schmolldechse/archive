using System.Text.Json.Serialization;

namespace Archive.Core.Storage;

/// <summary>Historical contract shared by URL captures (V1) and imported captures (V2).</summary>
internal sealed class LegacySnapshotManifestContract
{
    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("htmlObjectKey")]
    public string? HtmlObjectKey { get; init; }

    [JsonPropertyName("originalUrl")]
    public string? OriginalUrl { get; init; }

    [JsonPropertyName("resources")]
    public IReadOnlyList<LegacySnapshotResourceContract>? Resources { get; init; }
}

internal sealed class LegacySnapshotResourceContract
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; init; }

    [JsonPropertyName("objectKey")]
    public string? ObjectKey { get; init; }
}

/// <summary>Historical V3 contract whose interaction metadata was stored under <c>replay</c>.</summary>
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

internal sealed class SnapshotReplayMetadataV3Contract
{
    [JsonPropertyName("normalizer")]
    public string? Normalizer { get; init; }

    [JsonPropertyName("diagnostic")]
    public string? Diagnostic { get; init; }
}
