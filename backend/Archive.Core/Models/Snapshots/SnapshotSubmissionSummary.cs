using Archive.Core.Entities;
using Archive.Core.Entities.Processing;

namespace Archive.Core.Models.Snapshots;

/// <summary>A bounded view of submissions, independent of public resource URLs.</summary>
public sealed record SnapshotSubmissionSummary(Guid Id, string Title, string? SourceUrl,
    SourceType SourceType, DateTimeOffset CreatedAt, DateTimeOffset ModerationUpdatedAt,
    SnapshotModerationStatus ModerationStatus, ProcessingStatus ProcessingStatus,
    SnapshotQuality? Quality, string CreatorLabel, bool IsPublic);
