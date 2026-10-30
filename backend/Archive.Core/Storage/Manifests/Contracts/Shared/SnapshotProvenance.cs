using System.Text.Json.Serialization;

namespace Archive.Core.Storage.Manifests.Contracts.Shared;

/// <summary>Immutable source and derived-document provenance retained for a capture.</summary>
public sealed class SnapshotProvenance
{
    [JsonPropertyName("derivedDisplayDocument")]
    public bool DerivedDisplayDocument { get; init; }

    [JsonPropertyName("sourceObjectKey")]
    public required string SourceObjectKey { get; init; }

    [JsonPropertyName("sourceSha256")]
    public required string SourceSha256 { get; init; }

    [JsonPropertyName("indexSha256")]
    public required string IndexSha256 { get; init; }

    [JsonPropertyName("sourceUrl")]
    public string? SourceUrl { get; init; }

    [JsonPropertyName("sourceMimeType")]
    public required string SourceMimeType { get; init; }

    [JsonPropertyName("sourceStatusCode")]
    public int? SourceStatusCode { get; init; }

    [JsonPropertyName("capturedAt")]
    public DateTimeOffset CapturedAt { get; init; }

    [JsonPropertyName("documentState")]
    public required string DocumentState { get; init; }

    [JsonPropertyName("redirectChain")]
    public IReadOnlyList<string> RedirectChain { get; init; } = [];
}
