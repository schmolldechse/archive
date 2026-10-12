using Archive.Core.Entities;

namespace Archive.Core.Models.Snapshots;

public sealed record SnapshotDetails(Guid Id, SourceType SourceType, string? OriginUrl, string Title,
    string? Description, SnapshotQuality Quality, DateTimeOffset CreatedAt, long DurationMilliseconds,
    long StorageBytes, int ResourceCount, IReadOnlyList<string> Tags);
