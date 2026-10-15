using Archive.Core.Entities.Processing;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.SnapshotProcessingRepository;

internal sealed partial class SnapshotProcessingRepository
{
    public async Task<int> CleanupExpiredAsync(int batchSize, CancellationToken token = default)
    {
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
        var ids = await database.SnapshotProcessing.AsNoTracking()
            .Where(item => item.ExpiresAt <= DateTimeOffset.UtcNow)
            .OrderBy(item => item.ExpiresAt).ThenBy(item => item.SnapshotId)
            .Take(batchSize).Select(item => item.SnapshotId).ToListAsync(token);
        var removed = 0;
        foreach (var id in ids)
        {
            string? upload;
            await using (var transaction = await database.Database.BeginTransactionAsync(token))
            {
                var (snapshot, processing) = await LockAsync(database, id, token);
                if (snapshot is null || processing is null || processing.ExpiresAt is null
                    || processing.ExpiresAt > await TimestampAsync(database, token)
                    || processing.Status is ProcessingStatus.Queued or ProcessingStatus.Running) continue;
                upload = processing.UploadedObjectKey;
                if (snapshot.Quality is not null) database.SnapshotProcessing.Remove(processing);
                else database.Snapshots.Remove(snapshot);
                await database.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                removed++;
            }
            // Storage cleanup never holds row locks or determines publication truth.
            await DeleteUploadAsync(upload, token);
        }
        return removed;
    }
}
