namespace Archive.Core.Models.Snapshots;

/// <summary>History projection; reviewer identities are included only in the administrative view.</summary>
public sealed record SnapshotHistoryItem(Guid Id, long Sequence, DateTimeOffset CreatedAt,
    string? Actor, SnapshotHistoryContent Content);
