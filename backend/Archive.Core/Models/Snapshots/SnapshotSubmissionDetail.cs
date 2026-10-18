using Archive.Core.Entities;
using Archive.Core.Entities.Processing;

namespace Archive.Core.Models.Snapshots;

/// <summary>Private metadata and available actions for an owner or reviewer.</summary>
public sealed record SnapshotSubmissionDetail(SnapshotDetails Snapshot,
    SnapshotModerationStatus ModerationStatus, DateTimeOffset ModerationUpdatedAt,
    ProcessingStatus ProcessingStatus, string? LastDecisionReason, bool CanReview,
    bool CanResubmit, bool IsPublic, string ETag);
