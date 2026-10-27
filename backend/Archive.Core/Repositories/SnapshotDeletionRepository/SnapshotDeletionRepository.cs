using Archive.Core.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Archive.Core.Repositories.SnapshotDeletionRepository;

internal sealed class SnapshotDeletionRepository(
    DataContext database,
    IArchiveObjectStore objectStore,
    ILogger<SnapshotDeletionRepository> logger) : ISnapshotDeletionRepository
{
    public async Task<int> CleanupPendingAsync(int batchSize, CancellationToken token = default)
    {
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

        var now = DateTimeOffset.UtcNow;
        var ids = await database.SnapshotDeletions.AsNoTracking()
            .Where(item => item.CleanupAfter <= now && item.NextAttemptAt <= now)
            .OrderBy(item => item.NextAttemptAt)
            .ThenBy(item => item.SnapshotId)
            .Take(batchSize)
            .Select(item => item.SnapshotId)
            .ToArrayAsync(token);
        var removed = 0;

        foreach (var id in ids)
        {
            var item = await database.SnapshotDeletions.AsNoTracking()
                .SingleOrDefaultAsync(candidate => candidate.SnapshotId == id, token);
            if (item is null || item.CleanupAfter > DateTimeOffset.UtcNow ||
                item.NextAttemptAt > DateTimeOffset.UtcNow)
                continue;

            try
            {
                await objectStore.DeleteSnapshotObjectsAsync(
                    item.ContentPrefix, item.UploadedObjectKey, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Stored objects for deleted snapshot {SnapshotId} could not be removed", id);
                await RescheduleAsync(item.SnapshotId, item.AttemptCount, token);
                continue;
            }

            removed += await database.SnapshotDeletions
                .Where(candidate => candidate.SnapshotId == id)
                .ExecuteDeleteAsync(token);
        }

        return removed;
    }

    private Task<int> RescheduleAsync(Guid id, int previousAttemptCount, CancellationToken token)
    {
        var attemptCount = previousAttemptCount == int.MaxValue ? int.MaxValue : previousAttemptCount + 1;
        var exponent = Math.Min(Math.Max(attemptCount - 1, 0), 9);
        var delaySeconds = Math.Min(3_600, 5 * (1 << exponent));
        var nextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
        return database.SnapshotDeletions.Where(item => item.SnapshotId == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.AttemptCount, attemptCount)
                .SetProperty(item => item.NextAttemptAt, nextAttemptAt), token);
    }
}
