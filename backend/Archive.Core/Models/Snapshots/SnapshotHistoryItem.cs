using Archive.Core.Models.Users;

namespace Archive.Core.Models.Snapshots;

/// <summary>History projection; reviewer identities are included only in the administrative view.</summary>
public sealed record SnapshotHistoryItem(long Sequence, DateTimeOffset CreatedAt,
    PublicUser? Actor, SnapshotHistoryContent Content, SnapshotHistoryAttribution? Attribution = null);

/// <summary>API-safe attribution state resolved from durable user IDs stored by an attribution event.</summary>
public sealed record SnapshotHistoryAttribution(PublicUser? PreviousUser, PublicUser? User,
    bool Removed, bool Redacted);
