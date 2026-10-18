using Archive.Core.Models.Snapshots;
using Archive.Core.Entities;
using Archive.Core.Mapping;
using Archive.Core.Storage;
using Archive.Core.Uploads;
using Archive.Core.Options;
using Microsoft.EntityFrameworkCore;
using Archive.Core.Repositories.SnapshotHistoryRepository;

namespace Archive.Core.Repositories.SnapshotRepository;

internal sealed partial class SnapshotRepository(
    DataContext database, 
    IArchiveObjectStore objectStore,
    UploadFormatSelector uploadFormatSelector, 
    ArchiveOptions options, 
    ISnapshotHistoryRepository history) : ISnapshotRepository
{
    public async Task<SnapshotDetails?> GetByIdAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        return await database.Snapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId
                && snapshot.ModerationStatus == SnapshotModerationStatus.Approved
                && snapshot.Quality != null)
            .ProjectToDetails()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path, CancellationToken cancellationToken = default)
    {
        var snapshot = await database.Snapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId
                && snapshot.ModerationStatus == SnapshotModerationStatus.Approved
                && snapshot.Quality != null)
            .Select(snapshot => new { snapshot.ContentPrefix, snapshot.SourceType })
            .SingleOrDefaultAsync(cancellationToken);
        if (snapshot?.ContentPrefix is null) return new SnapshotContentResult.NotFound();

        return await objectStore.OpenSnapshotContentAsync(snapshot.ContentPrefix, snapshot.SourceType, path, cancellationToken);
    }

    public async Task<ArchiveObject?> OpenScreenshotAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        var prefix = await database.Snapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId
                && snapshot.ModerationStatus == SnapshotModerationStatus.Approved
                && snapshot.Quality != null)
            .Select(snapshot => snapshot.ContentPrefix)
            .SingleOrDefaultAsync(cancellationToken);
        return prefix is null ? null : await objectStore.GetAsync($"{prefix}/screenshot.png", cancellationToken);
    }

}
