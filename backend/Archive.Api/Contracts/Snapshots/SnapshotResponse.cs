using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Entities;
using Archive.Core.Models.Snapshots;
using Archive.Api.Contracts.Users;

namespace Archive.Api.Contracts.Snapshots;

[Description("Snapshot metadata and resource URLs.")]
public sealed class SnapshotResponse
{
    [JsonPropertyName("isPrivate")]
    [Description("Whether this snapshot is excluded from public discovery and ordinary public resource routes.")]
    public required bool IsPrivate { get; init; }

    [JsonPropertyName("id")]
    [Description("Snapshot identifier.")]
    public required Guid Id { get; init; }

    [JsonPropertyName("sourceUrl")]
    [Description("Original HTTP or HTTPS URL of the archived page.")]
    [Url]
    [StringLength(SnapshotValidation.MaxSourceUrlLength)]
    public required string? SourceUrl { get; init; }

    [JsonPropertyName("title")]
    [Description("Snapshot title.")]
    [StringLength(SnapshotValidation.MaxTitleLength)]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    [Description("Snapshot description. Null when no description was supplied.")]
    [StringLength(SnapshotValidation.MaxDescriptionLength)]
    public required string? Description { get; init; }

    [JsonPropertyName("sourceType")]
    [Description("Source used to create the snapshot.")]
    public required SourceType SourceType { get; init; }

    [JsonPropertyName("quality")]
    [Description("Completeness of the archived result. Null until capture storage has completed in private submission views.")]
    public required SnapshotQuality? Quality { get; init; }

    [JsonPropertyName("createdAt")]
    [Description("Snapshot creation time, including the UTC offset.")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("durationMilliseconds")]
    [Description("Processing duration in milliseconds. Null until capture storage has completed.")]
    [Range(0, long.MaxValue)]
    public required long? DurationMilliseconds { get; init; }

    [JsonPropertyName("storageBytes")]
    [Description("Snapshot storage size in bytes. Null until capture storage has completed.")]
    [Range(0, long.MaxValue)]
    public required long? StorageBytes { get; init; }

    [JsonPropertyName("resourceCount")]
    [Description("Number of archived resources. Null until capture storage has completed.")]
    [Range(0, int.MaxValue)]
    public required int? ResourceCount { get; init; }

    [JsonPropertyName("tags")]
    [Description("Tags assigned to the snapshot.")]
    public required IReadOnlyList<string> Tags { get; init; }

    [JsonPropertyName("screenshotUrl")]
    [Description("Screenshot URL. Null until capture storage has completed.")]
    [Url]
    public required string? ScreenshotUrl { get; init; }

    [JsonPropertyName("contentUrl")]
    [Description("Canonical archived start page URL on the credential-free snapshot origin. Public responses use the ordinary content path; authorized private responses use a short-lived capability path. Archived JavaScript executes when the client satisfies the required isolation policy. Null until capture storage has completed.")]
    [Url]
    public required string? ContentUrl { get; init; }

    [JsonPropertyName("createdBy")]
    [Description("Public profile of the submitting user. Null when no registered creator is available.")]
    public required PublicUserResponse? CreatedBy { get; init; }

    [JsonPropertyName("captureStartedAt")]
    [Description("Processing start time, including the UTC offset. Null before processing starts.")]
    public required DateTimeOffset? CaptureStartedAt { get; init; }

    [JsonPropertyName("captureCompletedAt")]
    [Description("Processing end time, including the UTC offset. Null before processing ends.")]
    public required DateTimeOffset? CaptureCompletedAt { get; init; }
}
