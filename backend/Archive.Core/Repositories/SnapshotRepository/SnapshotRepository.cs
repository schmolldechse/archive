using Archive.Core.Models.Snapshots;
using Archive.Core.Entities;
using Archive.Core.Mapping;
using Archive.Core.Storage;
using Archive.Core.Uploads;
using Archive.Core.Options;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.SnapshotRepository;

internal sealed partial class SnapshotRepository(DataContext database, IArchiveObjectStore objectStore,
    UploadFormatSelector uploadFormatSelector, ArchiveOptions options) : ISnapshotRepository
{
    public async Task<SnapshotDetails?> GetByIdAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        return await database.Snapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId)
            .Where(snapshot => snapshot.Quality != null || !database.SnapshotProcessing.Any(processing =>
                processing.SnapshotId == snapshot.Id && processing.ExpiresAt <= DateTimeOffset.UtcNow))
            .ProjectToDetails()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path, CancellationToken cancellationToken = default)
    {
        var snapshot = await database.Snapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId && snapshot.Quality != null)
            .Select(snapshot => new { snapshot.ContentPrefix, snapshot.SourceType })
            .SingleOrDefaultAsync(cancellationToken);
        if (snapshot is null) return new SnapshotContentResult.NotFound();

        var safePath = string.IsNullOrWhiteSpace(path) ? "index.html" : path.Replace('\\', '/').TrimStart('/');
        if (safePath.Contains("..", StringComparison.Ordinal)) return new SnapshotContentResult.InvalidPath();

        var stored = await objectStore.GetAsync($"{snapshot.ContentPrefix}/{safePath}", cancellationToken);
        return stored is null 
            ? new SnapshotContentResult.NotFound()
            : new SnapshotContentResult.Found(new(stored.Content, stored.ContentType,
                snapshot.SourceType != SourceType.Url && safePath.StartsWith("source.", StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<ArchiveObject?> OpenScreenshotAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        var prefix = await database.Snapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId && snapshot.Quality != null)
            .Select(snapshot => snapshot.ContentPrefix)
            .SingleOrDefaultAsync(cancellationToken);
        return prefix is null ? null : await objectStore.GetAsync($"{prefix}/screenshot.png", cancellationToken);
    }

}
