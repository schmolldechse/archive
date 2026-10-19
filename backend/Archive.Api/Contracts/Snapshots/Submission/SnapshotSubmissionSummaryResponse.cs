using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Api.Contracts.Users;
using Archive.Core.Entities;
using Archive.Core.Entities.Processing;
using Archive.Core.Models.Snapshots;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>Private summary of one snapshot submission.</summary>
[Description("Private summary of one snapshot submission and its current states.")]
public sealed class SnapshotSubmissionSummaryResponse
{
    [JsonPropertyName("id")]
    [Description("Snapshot identifier.")]
    public required Guid Id { get; init; }

    [JsonPropertyName("title")]
    [Description("Snapshot title.")]
    [StringLength(SnapshotValidation.MaxTitleLength)]
    public required string Title { get; init; }

    [JsonPropertyName("sourceUrl")]
    [Description("Original source URL. Null when unavailable.")]
    [Url]
    public required string? SourceUrl { get; init; }

    [JsonPropertyName("sourceType")]
    [Description("Source used to create the snapshot.")]
    public required SourceType SourceType { get; init; }

    [JsonPropertyName("createdAt")]
    [Description("Submission time, including the UTC offset.")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("moderationUpdatedAt")]
    [Description("Time of the latest moderation lifecycle change.")]
    public required DateTimeOffset ModerationUpdatedAt { get; init; }

    [JsonPropertyName("moderationStatus")]
    [Description("Current moderation status.")]
    public required SnapshotModerationStatus ModerationStatus { get; init; }

    [JsonPropertyName("processingStatus")]
    [Description("Current or final processing status.")]
    public required ProcessingStatus ProcessingStatus { get; init; }

    [JsonPropertyName("quality")]
    [Description("Stored capture quality. Null until capture storage has completed.")]
    public required SnapshotQuality? Quality { get; init; }

    [JsonPropertyName("captureCompletedAt")]
    [Description("Capture completion time, including the UTC offset. Null until a capture has ended.")]
    public required DateTimeOffset? CaptureCompletedAt { get; init; }

    [JsonPropertyName("durationMilliseconds")]
    [Description("Capture processing duration in milliseconds. Null until capture storage has completed.")]
    [Range(0, long.MaxValue)]
    public required long? DurationMilliseconds { get; init; }

    [JsonPropertyName("storageBytes")]
    [Description("Stored snapshot size in bytes. Null until capture storage has completed.")]
    [Range(0, long.MaxValue)]
    public required long? StorageBytes { get; init; }

    [JsonPropertyName("resourceCount")]
    [Description("Number of archived resources. Null until capture storage has completed.")]
    [Range(0, int.MaxValue)]
    public required int? ResourceCount { get; init; }

    [JsonPropertyName("createdBy")]
    [Description("Public information about the submitting account. Null for anonymous submissions.")]
    public required PublicUserResponse? CreatedBy { get; init; }
}
