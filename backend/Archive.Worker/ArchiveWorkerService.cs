using Archive.Core;
using Archive.Core.Entities;
using Archive.Core.Jobs;
using Archive.Core.Storage;
using Archive.Worker.Capture;
using Microsoft.EntityFrameworkCore;

namespace Archive.Worker;

public sealed class ArchiveWorkerService(
    IServiceScopeFactory scopeFactory,
    BrowserCaptureEngine captureEngine,
    IArchiveObjectStore objectStore,
    ILogger<ArchiveWorkerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var workerLease = await ArchiveWorkerLease.AcquireAsync(scopeFactory, stoppingToken);
        using var workerLifetime = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var workerToken = workerLifetime.Token;
        var leaseMonitor = workerLease.MonitorAsync(workerLifetime, logger);
        logger.LogInformation("Exklusive Worker-Lease übernommen");
        try
        {
            var leaseStartedAtUtc = DateTime.UtcNow;
            await MarkInterruptedJobsAsync(leaseStartedAtUtc, workerToken);

            while (!workerToken.IsCancellationRequested)
            {
                try
                {
                    var job = await ClaimNextJobAsync(leaseStartedAtUtc, workerToken);
                    if (job is not null)
                        await ProcessJobAsync(job, workerToken);
                    else
                        await Task.Delay(500, workerToken);
                }
                catch (OperationCanceledException) when (workerToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Fehler in der Archivierungswarteschlange");
                    await Task.Delay(1_000, workerToken);
                }
            }
        }
        finally
        {
            workerLifetime.Cancel();
            await leaseMonitor;
        }
    }

    private async Task<ArchiveJob?> ClaimNextJobAsync(DateTime leaseStartedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<DataContext>();
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var job = await database.ArchiveJobs
            .FromSqlInterpolated($"""
                SELECT * FROM processing.archive_jobs
                WHERE status = {JobStatus.Queued}
                   OR (status = {JobStatus.Running} AND started_at >= {leaseStartedAtUtc})
                ORDER BY CASE WHEN status = {JobStatus.Running} THEN 0 ELSE 1 END, created_at
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        if (job.Status == JobStatus.Queued)
        {
            job.Status = JobStatus.Running;
            job.StartedAt = DateTimeOffset.UtcNow;
            job.ProgressStep = ProgressStep.SourceCheck;
            job.ProgressPercent = 2;
            await database.SaveChangesAsync(cancellationToken);
        }
        else
        {
            logger.LogWarning("Running-Job {JobId} wird nach unklarem Claim-Commit wiederaufgenommen", job.Id);
        }
        await database.Entry(job).Collection(x => x.Tags).LoadAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return job;
    }

    private async Task ProcessJobAsync(ArchiveJob claimedJob, CancellationToken stoppingToken)
    {
        CaptureResult? storedCapture = null;
        var published = false;
        try
        {
            await UpdateProgressAsync(claimedJob.Id, ProgressStep.SourceCheck, 5, stoppingToken);
            var progress = new Progress<ProgressUpdate>(update => _ = UpdateProgressAsync(claimedJob.Id, update.Step, update.Percent, stoppingToken));
            var result = await CaptureWithCancellationMonitoringAsync(claimedJob, progress, stoppingToken);
            storedCapture = result;
            await UpdateProgressAsync(claimedJob.Id, ProgressStep.SavingSnapshot, 90, stoppingToken);

            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<DataContext>();
            await using var publication = await database.Database.BeginTransactionAsync(stoppingToken);
            var job = await database.ArchiveJobs
                .FromSqlInterpolated($"""
                    SELECT * FROM processing.archive_jobs
                    WHERE id = {claimedJob.Id}
                    FOR UPDATE
                    """)
                .SingleAsync(stoppingToken);
            await database.Entry(job).Collection(x => x.Tags).LoadAsync(stoppingToken);
            if (job.Status != JobStatus.Running)
            {
                await publication.CommitAsync(stoppingToken);
                return;
            }

            var projectKey = job.OriginUrl is not null
                ? UrlNormalizer.ProjectKey(job.OriginUrl)
                : $"html:{job.Id:N}";
            var project = await database.Projects.SingleOrDefaultAsync(x => x.ProjectKey == projectKey, stoppingToken);
            if (project is null)
            {
                project = new Project { ProjectKey = projectKey };
                database.Projects.Add(project);
            }

            var snapshot = new Snapshot
            {
                Id = job.Id,
                Project = project,
                SourceType = job.SourceType,
                OriginUrl = job.OriginUrl,
                Title = job.Title,
                Description = job.Description,
                Quality = result.Quality,
                CreatedAt = job.CreatedAt,
                CaptureStartedAt = job.StartedAt,
                CaptureCompletedAt = DateTimeOffset.UtcNow,
                DurationMilliseconds = result.DurationMilliseconds,
                StorageBytes = result.StorageBytes,
                ResourceCount = result.ResourceCount,
                ContentPrefix = result.ContentPrefix,
                SnapshotTags = []
            };

            foreach (var jobTag in job.Tags)
            {
                var tag = await database.Tags.SingleOrDefaultAsync(x => x.Value == jobTag.Value, stoppingToken);
                if (tag is null)
                {
                    tag = new Tag { Value = jobTag.Value };
                    database.Tags.Add(tag);
                }
                snapshot.SnapshotTags.Add(new SnapshotTag { Snapshot = snapshot, Tag = tag });
            }

            database.Snapshots.Add(snapshot);
            job.Status = JobStatus.Completed;
            job.ProgressStep = ProgressStep.Finished;
            job.ProgressPercent = 100;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await database.SaveChangesAsync(stoppingToken);
            await publication.CommitAsync(stoppingToken);
            published = true;
            logger.LogInformation("Snapshot {SnapshotId} veröffentlicht", snapshot.Id);
        }
        catch (CaptureCancelledException)
        {
            await SetTerminalStatusAsync(claimedJob.Id, JobStatus.Cancelled, "Die Archivierung wurde abgebrochen.", stoppingToken);
        }
        catch (CaptureDiscardedException exception)
        {
            await SetTerminalStatusAsync(claimedJob.Id, JobStatus.Discarded, exception.Message, stoppingToken);
        }
        catch (CaptureFailedException exception)
        {
            await SetTerminalStatusAsync(claimedJob.Id, JobStatus.Failed, exception.Message, stoppingToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Job {JobId} konnte nicht verarbeitet werden", claimedJob.Id);
            await SetTerminalStatusAsync(claimedJob.Id, JobStatus.Failed, "Die Archivierung ist unerwartet fehlgeschlagen.", stoppingToken);
        }
        finally
        {
            if (!published && storedCapture is not null &&
                await IsSafeToDeleteUnpublishedCaptureAsync(claimedJob.Id))
                await DeleteStoredCaptureAsync(storedCapture);
        }
    }

    private async Task<CaptureResult> CaptureWithCancellationMonitoringAsync(ArchiveJob job,
        IProgress<ProgressUpdate> progress, CancellationToken stoppingToken)
    {
        using var captureCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var captureTask = captureEngine.CaptureAsync(job, progress, captureCancellation.Token);
        try
        {
            while (!captureTask.IsCompleted)
            {
                await Task.WhenAny(captureTask, Task.Delay(250, stoppingToken));
                if (captureTask.IsCompleted)
                    break;

                await using var scope = scopeFactory.CreateAsyncScope();
                var database = scope.ServiceProvider.GetRequiredService<DataContext>();
                var status = await database.ArchiveJobs.AsNoTracking()
                    .Where(x => x.Id == job.Id)
                    .Select(x => (JobStatus?)x.Status)
                    .SingleOrDefaultAsync(stoppingToken);
                if (status != JobStatus.Running)
                {
                    captureCancellation.Cancel();
                    break;
                }
            }

            return await captureTask;
        }
        catch
        {
            captureCancellation.Cancel();
            try { await captureTask; }
            catch { }
            throw;
        }
    }

    private async Task DeleteStoredCaptureAsync(CaptureResult result)
    {
        foreach (var key in result.StoredObjectKeys.Reverse())
        {
            try { await objectStore.DeleteAsync(key, CancellationToken.None); }
            catch (Exception exception) { logger.LogWarning(exception, "Nicht veröffentlichbares Archivobjekt {ObjectKey} konnte nicht gelöscht werden", key); }
        }
    }

    private async Task<bool> IsSafeToDeleteUnpublishedCaptureAsync(Guid jobId)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<DataContext>();
            return !await database.Snapshots.AsNoTracking().AnyAsync(x => x.Id == jobId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            // After an ambiguous commit, deleting is more dangerous than leaving an
            // orphan: a durable snapshot may already reference these exact keys.
            logger.LogError(exception,
                "Archivobjekte für Job {JobId} bleiben erhalten, da der Veröffentlichungsstatus nicht geprüft werden konnte",
                jobId);
            return false;
        }
    }

    private async Task MarkInterruptedJobsAsync(DateTimeOffset startupAt, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<DataContext>();
        await database.ArchiveJobs.Where(x => x.Status == JobStatus.Running && x.StartedAt < startupAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, JobStatus.Aborted)
                .SetProperty(x => x.ErrorMessage, "Die Archivierung wurde nach einem Neustart verworfen.")
                .SetProperty(x => x.CompletedAt, DateTimeOffset.UtcNow), cancellationToken);
    }

    private async Task UpdateProgressAsync(Guid jobId, ProgressStep step, int percent, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<DataContext>();
        var job = await database.ArchiveJobs.SingleOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        if (job is null || job.Status != JobStatus.Running)
            return;
        job.ProgressStep = step;
        job.ProgressPercent = percent;
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task SetTerminalStatusAsync(Guid jobId, JobStatus status, string message, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<DataContext>();
        var completedAt = DateTime.UtcNow;
        await database.ArchiveJobs
            .Where(x => x.Id == jobId && x.Status == JobStatus.Running)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.ErrorMessage, message)
                .SetProperty(x => x.CompletedAt, completedAt), cancellationToken);
    }
}
