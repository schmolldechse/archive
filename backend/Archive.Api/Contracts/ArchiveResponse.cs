using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Entities;

namespace Archive.Api.Contracts;

public sealed record ArchiveResponse(
    [property: JsonPropertyName("id"), Description("Unique identifier of the archive."), Required] Guid Id,
    [property: JsonPropertyName("status"), Description("Current archive status."), Required] JobStatus Status,
    [property: JsonPropertyName("progressStep"), Description("Current archive progress step."), Required] ProgressStep ProgressStep,
    [property: JsonPropertyName("progressPercent"), Description("Progress percentage from 0 to 100."), Range(0, 100)] int ProgressPercent,
    [property: JsonPropertyName("message"), Description("Human-readable archive status message."), Required, StringLength(500)] string Message,
    [property: JsonPropertyName("queuePosition"), Description("One-based queue position while waiting.")] int? QueuePosition,
    [property: JsonPropertyName("snapshotId"), Description("Identifier of the created snapshot, when available.")] Guid? SnapshotId,
    [property: JsonPropertyName("snapshotQuality"), Description("Quality of the created snapshot, when available.")] SnapshotQuality? SnapshotQuality,
    [property: JsonPropertyName("errorMessage"), Description("Failure details, when the archive failed."), StringLength(10_000)] string? ErrorMessage,
    [property: JsonPropertyName("createdAt"), Description("Timestamp at which the archive was created, including its UTC offset."), Required] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("completedAt"), Description("Timestamp at which the archive completed, including its UTC offset.")] DateTimeOffset? CompletedAt,
    [property: JsonPropertyName("durationMilliseconds"), Description("Processing duration in milliseconds."), Range(0, long.MaxValue)] long DurationMilliseconds,
    [property: JsonPropertyName("storageBytes"), Description("Storage used by the archived snapshot in bytes."), Range(0, long.MaxValue)] long StorageBytes,
    [property: JsonPropertyName("resourceCount"), Description("Number of resources in the archived snapshot."), Range(0, int.MaxValue)] int ResourceCount);
