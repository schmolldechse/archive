namespace Archive.Core.Repositories.SnapshotDeletionRepository;

/// <summary>Completes durable object cleanup for snapshots whose relational records have been deleted.</summary>
public interface ISnapshotDeletionRepository
{
    Task<int> CleanupPendingAsync(int batchSize, CancellationToken cancellationToken = default);
}
