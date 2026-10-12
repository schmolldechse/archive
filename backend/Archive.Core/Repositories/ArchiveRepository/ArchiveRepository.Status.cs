using Archive.Core.Entities;
using Archive.Core.Models.Archive;
using Archive.Core.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.ArchiveRepository;

internal sealed partial class ArchiveRepository
{
    public async Task<ArchiveStatus?> GetByIdAsync(Guid archiveId, CancellationToken cancellationToken = default)
    {
        var job = await database.ArchiveJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(archive => archive.Id == archiveId, cancellationToken);
        return job is null ? null : await BuildStatusAsync(job, cancellationToken);
    }

    public async Task<ArchiveCancellationResult> CancelAsync(Guid archiveId, CancellationToken cancellationToken = default)
    {
        var completedAt = DateTimeOffset.UtcNow;
        var updated = await database.ArchiveJobs
            .Where(archive => archive.Id == archiveId && (archive.Status == JobStatus.Queued || archive.Status == JobStatus.Running))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(archive => archive.Status, JobStatus.Cancelled)
                .SetProperty(archive => archive.ErrorMessage, "The archive was cancelled.")
                .SetProperty(archive => archive.CompletedAt, completedAt), cancellationToken);
        if (updated == 0)
            return await database.ArchiveJobs.AnyAsync(archive => archive.Id == archiveId, cancellationToken)
                ? new ArchiveCancellationResult.NotCancellable() : new ArchiveCancellationResult.NotFound();
        return new ArchiveCancellationResult.Cancelled((await GetByIdAsync(archiveId, cancellationToken))!);
    }

    private async Task<ArchiveStatus> BuildStatusAsync(ArchiveJob archive, CancellationToken cancellationToken)
    {
        int? queuePosition = archive.Status == JobStatus.Queued
            ? await database.ArchiveJobs.CountAsync(job => job.Status == JobStatus.Queued && job.CreatedAt < archive.CreatedAt, cancellationToken) + 1
            : null;

        var message = archive.Status switch
        {
            JobStatus.Queued => "Waiting for archiving",
            JobStatus.Running => archive.ProgressStep switch
            {
                ProgressStep.SourceCheck => "Checking source",
                ProgressStep.LoadingPage => "Loading page",
                ProgressStep.LoadingDynamicContent => "Loading dynamic content",
                ProgressStep.SavingSnapshot => "Saving snapshot",
                _ => "Archiving in progress"
            },
            JobStatus.Completed => "Archiving completed",
            JobStatus.Cancelled => "Archiving cancelled",
            JobStatus.Discarded => "Archiving discarded because of a system limit",
            JobStatus.Aborted => "Archiving aborted after restart",
            JobStatus.Failed => "Archiving failed",
            _ => "Unknown status"
        };

        var snapshot = archive.Status == JobStatus.Completed
            ? await database.Snapshots
                .AsNoTracking()
                .Where(snapshot => snapshot.Id == archive.Id)
                .Select(snapshot => new { snapshot.Id, snapshot.Quality, snapshot.DurationMilliseconds, snapshot.StorageBytes, snapshot.ResourceCount })
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var creator = archive.CreatedByUserId is { } userId
            ? await database.Users.AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new ArchiveCreator(user.Handle!, user.DisplayName!, user.ProfileImage))
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        return new ArchiveStatus(
            Id: archive.Id,
            Status: archive.Status,
            ProgressStep: archive.ProgressStep,
            ProgressPercent: archive.ProgressPercent,
            Message: message,
            QueuePosition: queuePosition,
            SnapshotId: snapshot?.Id,
            SnapshotQuality: snapshot?.Quality,
            ErrorMessage: archive.ErrorMessage,
            CreatedAt: archive.CreatedAt,
            CompletedAt: archive.CompletedAt,
            DurationMilliseconds: snapshot?.DurationMilliseconds ?? 0,
            StorageBytes: snapshot?.StorageBytes ?? 0,
            ResourceCount: snapshot?.ResourceCount ?? 0,
            CreatedBy: creator);
    }
}
