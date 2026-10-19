using Archive.Core.Models.Users;

namespace Archive.Core.Models.Snapshots;

/// <summary>History projection; reviewer identities are included only in the administrative view.</summary>
public sealed record SnapshotHistoryItem(long Sequence, DateTimeOffset CreatedAt,
    PublicUser? Actor, SnapshotHistoryContent Content);
