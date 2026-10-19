using Archive.Core.Entities;

namespace Archive.Core.Models.Snapshots;

/// <summary>Current moderation state and its latest decision context.</summary>
public sealed record SnapshotModerationDetails(SnapshotModerationStatus Status,
    DateTimeOffset UpdatedAt, string? LastDecisionReason);
