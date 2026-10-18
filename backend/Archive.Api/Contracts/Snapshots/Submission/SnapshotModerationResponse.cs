using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Entities;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>Current moderation state and latest decision context.</summary>
[Description("Current moderation state and latest decision context.")]
public sealed class SnapshotModerationResponse
{
    [JsonPropertyName("status")]
    [Description("Current moderation status.")]
    public required SnapshotModerationStatus Status { get; init; }

    [JsonPropertyName("updatedAt")]
    [Description("Time of the latest moderation lifecycle change.")]
    public required DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("lastDecisionReason")]
    [Description("Optional reason supplied with the latest moderation decision.")]
    [StringLength(2048)]
    public required string? LastDecisionReason { get; init; }
}
