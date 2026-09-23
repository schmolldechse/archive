using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Entities;

namespace Archive.Api.Contracts;

public sealed record SnapshotResponse(
    [property: JsonPropertyName("id"), Description("Unique identifier of the snapshot."), Required] Guid Id,
    [property: JsonPropertyName("sourceType"), Description("Format used to create the snapshot."), Required] SourceType SourceType,
    [property: JsonPropertyName("sourceUrl"), Description("Archived public source URL."), Url, StringLength(2_048)] string? SourceUrl,
    [property: JsonPropertyName("originalLink"), Description("Original public URL for uploaded content."), Url, StringLength(2_048)] string? OriginalLink,
    [property: JsonPropertyName("title"), Description("Snapshot title."), Required, StringLength(500)] string Title,
    [property: JsonPropertyName("description"), Description("Optional snapshot description."), StringLength(10_000)] string? Description,
    [property: JsonPropertyName("quality"), Description("Completeness of the snapshot."), Required] SnapshotQuality Quality,
    [property: JsonPropertyName("createdAt"), Description("Timestamp at which the snapshot was created, including its UTC offset."), Required] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("durationMilliseconds"), Description("Archive duration in milliseconds."), Range(0, long.MaxValue)] long DurationMilliseconds,
    [property: JsonPropertyName("storageBytes"), Description("Storage used by the snapshot in bytes."), Range(0, long.MaxValue)] long StorageBytes,
    [property: JsonPropertyName("resourceCount"), Description("Number of resources in the archived snapshot."), Range(0, int.MaxValue)] int ResourceCount,
    [property: JsonPropertyName("tags"), Description("Tags assigned to the snapshot."), Required] IReadOnlyList<string> Tags,
    [property: JsonPropertyName("screenshotUrl"), Description("Public URL of the snapshot screenshot."), Required, Url] string ScreenshotUrl,
    [property: JsonPropertyName("contentUrl"), Description("Public URL of the archived start page."), Required, Url] string ContentUrl);
