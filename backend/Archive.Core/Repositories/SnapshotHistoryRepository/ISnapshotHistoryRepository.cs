using Archive.Core.Entities;
using Archive.Core.Models.Snapshots.History;

namespace Archive.Core.Repositories.SnapshotHistoryRepository;

/// <summary>Appends typed history events within the caller's snapshot transaction.</summary>
public interface ISnapshotHistoryRepository
{
    /// <summary>Assigns a per-snapshot position and tracks the entry without saving or committing.</summary>
    Task<SnapshotHistoryEntry> AppendAsync(Snapshot snapshot, SnapshotHistoryData data, Guid? actorUserId = null, CancellationToken cancellationToken = default);
}
