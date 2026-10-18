using Archive.Core.Entities;
using Archive.Core.Entities.Processing;

namespace Archive.Core.Models.Snapshots;

/// <summary>Private snapshot metadata, moderation state and processing state.</summary>
public sealed record SnapshotSubmissionDetail(SnapshotDetails Snapshot,
    SnapshotModerationDetails Moderation, ProcessingStatus ProcessingStatus, string ETag);
