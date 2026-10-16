using Archive.Core.Entities;
using Archive.Core.Models.Users;

namespace Archive.Core.Models.Snapshots;

public sealed record SnapshotDetails(Guid Id, SourceType SourceType, string? SourceUrl, string Title,
    string? Description, SnapshotQuality? Quality, DateTimeOffset CreatedAt, long? DurationMilliseconds,
    long? StorageBytes, int? ResourceCount, IReadOnlyList<string> Tags, ArchiveCreator? CreatedBy,
    DateTimeOffset? CaptureStartedAt, DateTimeOffset? CaptureCompletedAt);
