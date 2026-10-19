namespace Archive.Core.Models.Snapshots;

/// <summary>Explicit transactional outcomes for moderation and owner resubmission.</summary>
public enum SnapshotReviewOutcome { Updated, NotFound, Forbidden, Stale, Conflict }
