using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Snapshots.Creation;

[Description("An accepted snapshot submission.")]
public sealed record SnapshotCreationResponse
{
    [JsonPropertyName("id")]
    [Description("Identifier of the submitted snapshot.")]
    public required Guid Id { get; init; }

    [JsonPropertyName("statusUrl")]
    [Description("Relative URL of the processing status endpoint.")]
    public required string StatusUrl { get; init; }

    [JsonPropertyName("snapshotUrl")]
    [Description("Relative URL of the snapshot metadata endpoint.")]
    public required string SnapshotUrl { get; init; }
}
