using Archive.Core.Entities;
using Archive.Core.Models.Snapshots;
using Archive.Core.Models.Snapshots.History;

namespace Archive.Core.Repositories.SnapshotHistoryRepository;

/// <summary>Appends typed history events within the caller's snapshot transaction.</summary>
public interface ISnapshotHistoryRepository
{
    /// <summary>Assigns a per-snapshot position and tracks the entry without saving or committing.</summary>
    Task<SnapshotHistoryEntry> AppendAsync(Snapshot snapshot, SnapshotHistoryData data, Guid? actorUserId = null, CancellationToken cancellationToken = default);

    /// <summary>Reads the known history events visible to the snapshot owner.</summary>
    Task<SnapshotHistoryPage?> ReadOwnAsync(Guid userId, Guid snapshotId, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Reads every history event for an authorized administrator.</summary>
    Task<SnapshotHistoryPage?> ReadAdminAsync(Guid snapshotId, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Reads a fail-closed, sanitized history for an approved stored snapshot.</summary>
    Task<SnapshotHistoryPage?> ReadPublicAsync(Guid snapshotId, int page, int pageSize,
        CancellationToken cancellationToken = default);
}
