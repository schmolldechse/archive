using Archive.Core.Entities;
using Archive.Core.Mapping;
using Archive.Core.Models.Snapshots.History;
using Microsoft.EntityFrameworkCore;
using Archive.Core.Serialization;

namespace Archive.Core.Repositories.SnapshotHistoryRepository;

/// <summary>Serializes history appends per snapshot using the shared scoped database context.</summary>
internal sealed class SnapshotHistoryRepository(DataContext database) : ISnapshotHistoryRepository
{
    public async Task<SnapshotHistoryEntry> AppendAsync(Snapshot snapshot, SnapshotHistoryData data, Guid? actorUserId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (database.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Snapshot history must be appended within a transaction.");

        var snapshotState = database.Entry(snapshot).State;
        if (snapshotState == EntityState.Detached)
            throw new InvalidOperationException("Track the snapshot before appending its history.");

        var history = SnapshotHistoryMapper.ToEntity(snapshot, SnapshotHistorySerializer.Serialize(data), actorUserId);

        long persistedSequence = 0;
        if (snapshotState != EntityState.Added)
        {
            // Lock the parent even when it has no history; concurrent appends wait for this transaction.
            await database.Database
                .SqlQuery<Guid>($"""SELECT id AS "Value" FROM archive.snapshots WHERE id = {snapshot.Id} FOR UPDATE""")
                .SingleAsync(cancellationToken);
            persistedSequence = await database.SnapshotHistory
                .Where(entry => entry.SnapshotId == snapshot.Id)
                .MaxAsync(entry => (long?)entry.Sequence, cancellationToken) ?? 0;
        }

        // Pending entries also reserve positions, allowing several events to share one SaveChanges call.
        var pendingSequence = database.SnapshotHistory.Local
            .Where(entry => entry.SnapshotId == snapshot.Id)
            .Max(entry => (long?)entry.Sequence) ?? 0;
        history.Sequence = checked(Math.Max(persistedSequence, pendingSequence) + 1);
        history.CreatedAt = DateTimeOffset.UtcNow;

        database.SnapshotHistory.Add(history);
        return history;
    }
}
