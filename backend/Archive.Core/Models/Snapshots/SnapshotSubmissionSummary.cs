using Archive.Core.Entities;
using Archive.Core.Entities.Processing;
using Archive.Core.Models.Users;

namespace Archive.Core.Models.Snapshots;

/// <summary>A bounded view of submissions, independent of public resource URLs.</summary>
public sealed record SnapshotSubmissionSummary(Guid Id, string Title, string? SourceUrl,
    SourceType SourceType, DateTimeOffset CreatedAt, DateTimeOffset ModerationUpdatedAt,
    SnapshotModerationStatus ModerationStatus, ProcessingStatus ProcessingStatus,
    SnapshotQuality? Quality, DateTimeOffset? CaptureCompletedAt, long? DurationMilliseconds,
    long? StorageBytes, int? ResourceCount, PublicUser? CreatedBy);
