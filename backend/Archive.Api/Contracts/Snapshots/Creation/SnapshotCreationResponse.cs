using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Snapshots.Creation;

[Description("An accepted snapshot submission.")]
public sealed record SnapshotCreationResponse
{
    [JsonPropertyName("isPrivate")]
    [Description("Whether this submission is excluded from public discovery and public snapshot routes.")]
    public required bool IsPrivate { get; init; }

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
    [Description("Relative metadata URL. Private submissions use the authenticated owner route.")]
    public required string SnapshotUrl { get; init; }
}
