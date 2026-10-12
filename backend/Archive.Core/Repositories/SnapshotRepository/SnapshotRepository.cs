using Archive.Core.Models.Snapshots;
using Archive.Core.Entities;
using Archive.Core.Storage;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.SnapshotRepository;

internal sealed partial class SnapshotRepository(DataContext database, IArchiveObjectStore objectStore) : ISnapshotRepository
{
    public async Task<SnapshotDetails?> GetByIdAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        var snapshot = await database.Snapshots
            .AsNoTracking()
            .Include(snapshot => snapshot.SnapshotTags)
            .ThenInclude(snapshotTag => snapshotTag.Tag)
            .SingleOrDefaultAsync(snapshot => snapshot.Id == snapshotId, cancellationToken);
        if (snapshot is null) return null;

        return ToDetails(snapshot);
    }

    public async Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path, CancellationToken cancellationToken = default)
    {
        var snapshot = await database.Snapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId)
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
            .Where(snapshot => snapshot.Id == snapshotId)
            .Select(snapshot => snapshot.ContentPrefix)
            .SingleOrDefaultAsync(cancellationToken);
        return prefix is null ? null : await objectStore.GetAsync($"{prefix}/screenshot.png", cancellationToken);
    }

    private static SnapshotDetails ToDetails(Snapshot snapshot) => new(
        snapshot.Id, snapshot.SourceType, snapshot.OriginUrl, snapshot.Title, snapshot.Description, snapshot.Quality,
        snapshot.CreatedAt, snapshot.DurationMilliseconds, snapshot.StorageBytes, snapshot.ResourceCount,
        snapshot.SnapshotTags.Select(item => item.Tag.Value).Order().ToArray());
}
