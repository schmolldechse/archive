using Archive.Core.Entities;
using Archive.Core.Models.Users;

namespace Archive.Core.Models.Archive;

public sealed record ArchiveStatus(Guid Id, JobStatus Status, ProgressStep ProgressStep, int ProgressPercent,
    string Message, int? QueuePosition, Guid? SnapshotId, SnapshotQuality? SnapshotQuality, string? ErrorMessage,
    DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, long DurationMilliseconds, long StorageBytes, int ResourceCount,
    ArchiveCreator? CreatedBy);
