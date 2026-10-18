using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Snapshots.Creation;

[Description("An accepted snapshot submission.")]
public sealed record SnapshotCreationResponse
{
    [JsonPropertyName("moderationStatus")]
    [Description("Initial review state. Public metadata remains unavailable until capture and approval are complete.")]
    public required Core.Entities.SnapshotModerationStatus ModerationStatus { get; init; }

    [JsonPropertyName("id")]
    [Description("Identifier of the submitted snapshot.")]
    public required Guid Id { get; init; }

    [JsonPropertyName("statusUrl")]
    [Description("Relative URL of the processing status endpoint.")]
    public required string StatusUrl { get; init; }

    [JsonPropertyName("snapshotUrl")]
    [Description("Relative public metadata URL. Returns 404 until capture storage and approval are complete.")]
    public required string SnapshotUrl { get; init; }
}
