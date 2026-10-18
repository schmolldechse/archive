using System.ComponentModel;
using System.Text.Json.Serialization;
using Archive.Core.Entities.Processing;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>Private snapshot metadata and lifecycle state.</summary>
[Description("Private snapshot metadata and lifecycle state for its owner or a moderator.")]
public sealed class SnapshotSubmissionDetailResponse
{
    [JsonPropertyName("snapshot")]
    [Description("Snapshot metadata with resource URLs scoped to the current private endpoint.")]
    public required SnapshotResponse Snapshot { get; init; }

    [JsonPropertyName("moderation")]
    [Description("Current moderation state and latest decision context.")]
    public required SnapshotModerationResponse Moderation { get; init; }

    [JsonPropertyName("processingStatus")]
    [Description("Current or final processing status.")]
    public required ProcessingStatus ProcessingStatus { get; init; }
}
