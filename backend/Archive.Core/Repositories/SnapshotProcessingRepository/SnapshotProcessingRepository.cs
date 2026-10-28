using Archive.Core.Entities;
using Archive.Core.Entities.Processing;
using Archive.Core.Mapping;
using Archive.Core.Models.Processing;
using Archive.Core.Models.Snapshots.History;
using Archive.Core.Options;
using Archive.Core.Processing;
using Archive.Core.Repositories.SnapshotHistoryRepository;
using Archive.Core.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Archive.Core.Repositories.SnapshotProcessingRepository;

internal sealed partial class SnapshotProcessingRepository(
    DataContext database,
    ProcessingOptions options,
    IArchiveObjectStore objectStore,
    ILogger<SnapshotProcessingRepository> logger,
    ISnapshotHistoryRepository history) : ISnapshotProcessingRepository
{
    public async Task<SnapshotProcessingState?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        // One statement provides a consistent publication/state/rank observation.
        var row = await (from snapshot in database.Snapshots.AsNoTracking()
                         join processing in database.SnapshotProcessing.AsNoTracking()
                             on snapshot.Id equals processing.SnapshotId into records
                         from processing in records.DefaultIfEmpty()
                         where snapshot.Id == id
                         select new
                         {
                             Snapshot = snapshot,
                             Processing = processing,
                             Expired = processing != null && processing.ExpiresAt <= DateTimeOffset.UtcNow,
                             Position = processing != null && processing.Status == ProcessingStatus.Queued
                                 ? (int?)(1 + database.SnapshotProcessing.Count(waiting =>
                                     waiting.Status == ProcessingStatus.Queued && waiting.QueueSequence < processing.QueueSequence))
                                 : null
                         }).SingleOrDefaultAsync(token);
        if (row is null) return null;
        if (row.Snapshot.Quality is not null) return Completed(row.Snapshot);
        if (row.Processing?.Status == ProcessingStatus.Completed)
            throw new ProcessingInconsistentException("Completed processing has no stored capture.");
        if (row.Processing is null || row.Expired) return null;
        return ToState(row.Processing, row.Snapshot, row.Position);
    }

    public async Task<ProcessingCancellation> CancelAsync(Guid id, CancellationToken token = default)
    {
        string? upload;
        SnapshotProcessingState state;
        await using (var transaction = await database.Database.BeginTransactionAsync(token))
        {
            var (snapshot, processing) = await LockAsync(database, id, token);
            if (snapshot is null) return new(CancellationOutcome.Missing, null);
            if (snapshot.Quality is not null)
                return new(CancellationOutcome.NotCancellable, Completed(snapshot));
            if (processing is null) return new(CancellationOutcome.Missing, null);
            var now = await TimestampAsync(database, token);
            if (processing.ExpiresAt <= now) return new(CancellationOutcome.Missing, null);
            if (processing.Status == ProcessingStatus.Queued)
            {
                SetTerminal(processing, ProcessingStatus.Cancelled, now);
                snapshot.CaptureStartedAt = processing.StartedAt;
                snapshot.CaptureCompletedAt = now;
                await history.AppendAsync(snapshot, new SnapshotProcessingFinishedData(ProcessingStatus.Cancelled),
                    cancellationToken: token);
            }
            else if (processing.Status == ProcessingStatus.Running)
                processing.CancelRequested = true;
            else if (processing.Status != ProcessingStatus.Cancelled)
                return new(CancellationOutcome.NotCancellable, ToState(processing, snapshot));
            await database.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            state = ToState(processing, snapshot);
            upload = processing.Status == ProcessingStatus.Cancelled ? processing.UploadedObjectKey : null;
        }
        await DeleteUploadAsync(upload, token);
        return new(CancellationOutcome.Accepted, state);
    }

    public Task<bool> CanContinueAsync(Guid id, Guid owner, CancellationToken token = default) =>
        database.SnapshotProcessing.AsNoTracking().AnyAsync(item => item.SnapshotId == id
            && item.Status == ProcessingStatus.Running && item.OwnerToken == owner && !item.CancelRequested, token);

    public async Task<bool> UpdateStepAsync(Guid id, Guid owner, ProgressStep step, CancellationToken token = default)
    {
        if (step is < ProgressStep.SourceCheck or >= ProgressStep.Finished)
            throw new ArgumentOutOfRangeException(nameof(step));
        // PostgreSQL enum ordering follows its declaration, not the workflow's phase order.
        return await database.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE processing.snapshot_processing
            SET progress_step = {step}
            WHERE snapshot_id = {id} AND status = 'RUNNING'
                AND owner_token = {owner} AND NOT cancel_requested
                AND CASE progress_step
                    WHEN 'SOURCE_CHECK' THEN 0 WHEN 'LOADING_PAGE' THEN 1
                    WHEN 'LOADING_DYNAMIC_CONTENT' THEN 2 WHEN 'SAVING_SNAPSHOT' THEN 3
                    ELSE 4 END < {(int)step}
            """, token) > 0;
    }

    private void SetTerminal(SnapshotProcessing processing, ProcessingStatus status, DateTimeOffset now)
    {
        processing.Status = status;
        if (status == ProcessingStatus.Completed) processing.ProgressStep = ProgressStep.Finished;
        processing.OwnerToken = null;
        processing.CancelRequested = false;
        processing.CompletedAt ??= now;
        processing.ExpiresAt ??= now.AddSeconds(options.TerminalRetentionSeconds);
    }

    private static SnapshotProcessingState ToState(SnapshotProcessing item, Snapshot snapshot, int? position = null) =>
        SnapshotProcessingModelMapper.ToState(item, position,
            item.Status is ProcessingStatus.Queued or ProcessingStatus.Running && !item.CancelRequested,
            snapshot.ModerationStatus, snapshot.IsPrivate);

    private static SnapshotProcessingState Completed(Snapshot snapshot) =>
        new(snapshot.Id, ProcessingStatus.Completed, ProgressStep.Finished, null, false, false,
            snapshot.CaptureStartedAt, snapshot.ModerationStatus, snapshot.IsPrivate);

    // Every operation locking both tables follows this order, including recovery and cleanup.
    private static async Task<(Snapshot? Snapshot, SnapshotProcessing? Processing)> LockAsync(
        DataContext db, Guid id, CancellationToken token)
    {
        db.ChangeTracker.Clear();
        var snapshot = await db.Snapshots.FromSqlInterpolated(
            $"SELECT * FROM archive.snapshots WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(token);
        var processing = snapshot is null ? null : await db.SnapshotProcessing.FromSqlInterpolated(
            $"SELECT * FROM processing.snapshot_processing WHERE snapshot_id = {id} FOR UPDATE").SingleOrDefaultAsync(token);
        return (snapshot, processing);
    }

    private static Task<DateTimeOffset> TimestampAsync(DataContext db, CancellationToken token) =>
        db.Database.SqlQuery<DateTimeOffset>($"SELECT clock_timestamp() AS \"Value\"").SingleAsync(token);

    private async Task DeleteUploadAsync(string? key, CancellationToken token = default)
    {
        if (key is null) return;
        try { await objectStore.DeleteAsync(key, token); }
        catch (Exception exception) { logger.LogWarning(exception, "Temporary upload {ObjectKey} could not be removed", key); }
    }
}
