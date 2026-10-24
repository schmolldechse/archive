using Archive.Core.Entities.Processing;
using Archive.Core.Mapping;
using Archive.Core.Models.Processing;
using Archive.Core.Models.Snapshots.History;
using Archive.Core.Processing;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.SnapshotProcessingRepository;

internal sealed partial class SnapshotProcessingRepository
{
    public async Task<ClaimedSnapshot?> TryClaimNextAsync(Guid owner, CancellationToken token = default)
    {
        if (owner == Guid.Empty) throw new ArgumentException("A claim needs an owner token.", nameof(owner));
        var rows = await database.SnapshotProcessing.FromSqlInterpolated($"""
            WITH candidate AS (
                SELECT snapshot_id FROM processing.snapshot_processing
                WHERE status = 'QUEUED' ORDER BY queue_sequence LIMIT 1 FOR UPDATE SKIP LOCKED
            )
            UPDATE processing.snapshot_processing AS p
            SET status = 'RUNNING', owner_token = {owner}, started_at = clock_timestamp(),
                progress_step = 'SOURCE_CHECK'
            FROM candidate AS c
            WHERE p.snapshot_id = c.snapshot_id AND p.status = 'QUEUED'
            RETURNING p.*
            """).AsNoTracking().ToListAsync(token);
        var item = rows.SingleOrDefault();
        if (item is null) return null;
        var snapshot = await database.Snapshots.AsNoTracking().SingleAsync(s => s.Id == item.SnapshotId, token);
        if (snapshot.Quality is not null)
            throw new ProcessingInconsistentException("A stored snapshot was queued for capture.");
        return new(SnapshotModelMapper.ToCaptureInput(snapshot, item.UploadedObjectKey), owner,
            item.StartedAt ?? throw new ProcessingInconsistentException("A claimed snapshot has no start time."));
    }

    public async Task<bool> CompleteCaptureAsync(Guid id, Guid owner, SnapshotCaptureResult result, CancellationToken token = default)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var (snapshot, processing) = await LockAsync(database, id, token);
        if (snapshot?.Quality is not null) return true;
        if (snapshot is null || processing is null || processing.Status != ProcessingStatus.Running
            || processing.OwnerToken != owner || processing.CancelRequested) return false;
        var now = await TimestampAsync(database, token);
        snapshot.Quality = result.Quality;
        snapshot.ContentPrefix = result.ContentPrefix;
        snapshot.DurationMilliseconds = result.DurationMilliseconds;
        snapshot.StorageBytes = result.StorageBytes;
        snapshot.ResourceCount = result.ResourceCount;
        snapshot.ReplayVersion = result.ReplayVersion;
        snapshot.CaptureStartedAt = processing.StartedAt;
        snapshot.CaptureCompletedAt = now;
        await history.AppendAsync(snapshot,
            new SnapshotStoredData(snapshot.Quality, snapshot.DurationMilliseconds,
                snapshot.StorageBytes, snapshot.ResourceCount), cancellationToken: token);
        SetTerminal(processing, ProcessingStatus.Completed, now);
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return true;
    }

    public async Task<ProcessingCompletion> FinishAsync(Guid id, Guid owner, ProcessingStatus status,
        CancellationToken token = default)
    {
        if (status is not (ProcessingStatus.Failed or ProcessingStatus.Discarded or ProcessingStatus.Cancelled))
            throw new ArgumentOutOfRangeException(nameof(status));
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var (snapshot, processing) = await LockAsync(database, id, token);
        // Re-read publication after an ambiguous commit before allowing any result-object cleanup.
        if (snapshot?.Quality is not null) return ProcessingCompletion.Stored;
        if (snapshot is null) return ProcessingCompletion.NotStored;
        if (processing is null) throw new ProcessingInconsistentException("Snapshot processing is missing.");
        if (processing.Status == ProcessingStatus.Completed)
            throw new ProcessingInconsistentException("Completed processing has no stored result.");
        if (processing.Status is not (ProcessingStatus.Running or ProcessingStatus.Queued))
            return ProcessingCompletion.NotStored;
        if (processing.Status != ProcessingStatus.Running || processing.OwnerToken != owner)
            return ProcessingCompletion.OwnershipLost;
        if (processing.CancelRequested) status = ProcessingStatus.Cancelled;
        SetTerminal(processing, status, await TimestampAsync(database, token));
        snapshot.CaptureStartedAt = processing.StartedAt;
        snapshot.CaptureCompletedAt = processing.CompletedAt;
        await history.AppendAsync(snapshot, new SnapshotProcessingFinishedData(status), cancellationToken: token);
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return ProcessingCompletion.NotStored;
    }

    public async Task<IReadOnlyList<string>> RecoverInterruptedAsync(CancellationToken token = default)
    {
        var ids = await database.SnapshotProcessing.AsNoTracking()
            .Where(item => item.Status == ProcessingStatus.Running).Select(item => item.SnapshotId).ToListAsync(token);
        var uploads = new List<string>();
        foreach (var id in ids)
        {
            await using var transaction = await database.Database.BeginTransactionAsync(token);
            var (snapshot, processing) = await LockAsync(database, id, token);
            if (snapshot is null || processing?.Status != ProcessingStatus.Running) continue;
            var status = snapshot.Quality is not null ? ProcessingStatus.Completed
                : processing.CancelRequested ? ProcessingStatus.Cancelled : ProcessingStatus.Aborted;
            SetTerminal(processing, status, await TimestampAsync(database, token));
            snapshot.CaptureStartedAt = processing.StartedAt;
            snapshot.CaptureCompletedAt = processing.CompletedAt;
            SnapshotHistoryData historyData = status == ProcessingStatus.Completed
                ? new SnapshotStoredData(snapshot.Quality, snapshot.DurationMilliseconds,
                    snapshot.StorageBytes, snapshot.ResourceCount)
                : new SnapshotProcessingFinishedData(status);
            await history.AppendAsync(snapshot, historyData, cancellationToken: token);
            await database.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            if (processing.UploadedObjectKey is { } upload) uploads.Add(upload);
        }
        // The worker deletes uploads after releasing the lease's database-operation gate.
        return uploads;
    }
}
