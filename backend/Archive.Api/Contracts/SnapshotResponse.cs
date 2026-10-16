using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Entities;
using Archive.Core.Models.Snapshots;

namespace Archive.Api.Contracts;

public sealed record SnapshotResponse(
    [property: JsonPropertyName("id"), Description("Unique identifier of the snapshot."), Required] Guid Id,
    [property: JsonPropertyName("sourceUrl"), Description("Public source URL for a captured or uploaded page, up to 4,096 characters."), Url, StringLength(SnapshotValidation.MaxSourceUrlLength)] string? SourceUrl,
    [property: JsonPropertyName("title"), Description("Snapshot title."), Required, StringLength(SnapshotValidation.MaxTitleLength)] string Title,
    [property: JsonPropertyName("description"), Description("Optional snapshot description, up to 2,048 characters."), StringLength(SnapshotValidation.MaxDescriptionLength)] string? Description,
    [property: JsonPropertyName("sourceType"), Required] SourceType SourceType,
    [property: JsonPropertyName("quality"), Description("Completeness of the published result. Null while processing.")] SnapshotQuality? Quality,
    [property: JsonPropertyName("createdAt"), Description("Timestamp at which the snapshot was created, including its UTC offset."), Required] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("durationMilliseconds"), Description("Archive duration in milliseconds."), Range(0, long.MaxValue)] long? DurationMilliseconds,
    [property: JsonPropertyName("storageBytes"), Description("Storage used by the snapshot in bytes."), Range(0, long.MaxValue)] long? StorageBytes,
    [property: JsonPropertyName("resourceCount"), Description("Number of resources in the archived snapshot."), Range(0, int.MaxValue)] int? ResourceCount,
    [property: JsonPropertyName("tags"), Description("Tags assigned to the snapshot, up to 128 characters each."), Required] IReadOnlyList<string> Tags,
    [property: JsonPropertyName("screenshotUrl"), Description("Public screenshot URL after publication."), Url] string? ScreenshotUrl,
    [property: JsonPropertyName("contentUrl"), Description("Public start page URL after publication."), Url] string? ContentUrl,
    [property: JsonPropertyName("createdBy"), Description("Public profile of the submitting user.")] ArchiveCreatorResponse? CreatedBy,
    [property: JsonPropertyName("captureStartedAt")] DateTimeOffset? CaptureStartedAt,
    [property: JsonPropertyName("captureCompletedAt")] DateTimeOffset? CaptureCompletedAt);
