using Archive.Core.Models.Snapshots;
using Archive.Core.Entities;
using Archive.Core.Mapping;
using Archive.Core.Storage;
using Archive.Core.Uploads;
using Archive.Core.Options;
using Microsoft.EntityFrameworkCore;
using Archive.Core.Repositories.SnapshotHistoryRepository;
using System.Text.Json;

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

    public Task<SnapshotHistoryPage?> HistoryAsync(Guid snapshotId, int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        history.ReadPublicAsync(snapshotId, page, pageSize, cancellationToken);

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

    /// <summary>Returns only resources explicitly authorized by a compatible public replay manifest.</summary>
    public async Task<SnapshotReplayContentResult> OpenReplayContentAsync(Guid snapshotId, string? path,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await database.Snapshots
            .AsNoTracking()
            .Where(item => item.Id == snapshotId
                && item.ModerationStatus == SnapshotModerationStatus.Approved
                && item.Quality != null
                && item.ReplayVersion == SnapshotReplayManifest.CurrentReplayVersion)
            .Select(item => new { item.ContentPrefix, item.ReplayVersion })
            .SingleOrDefaultAsync(cancellationToken);
        if (snapshot?.ContentPrefix is null)
            return new SnapshotReplayContentResult.NotFound();

        var safePath = string.IsNullOrWhiteSpace(path) ? "index.html" : path.Replace('\\', '/').TrimStart('/');
        if (safePath.Contains("..", StringComparison.Ordinal) || safePath.Any(char.IsControl))
            return new SnapshotReplayContentResult.InvalidPath();

        var storedManifest = await objectStore.GetAsync($"{snapshot.ContentPrefix}/manifest.json", cancellationToken);
        if (storedManifest is null)
            return new SnapshotReplayContentResult.NotFound();

        SnapshotReplayManifest? manifest;
        try
        {
            await using (storedManifest.Content)
                manifest = await JsonSerializer.DeserializeAsync<SnapshotReplayManifest>(storedManifest.Content,
                    cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return new SnapshotReplayContentResult.NotFound();
        }

        if (manifest is null || manifest.Version != SnapshotReplayManifest.CurrentManifestVersion ||
            manifest.Replay.Version != snapshot.ReplayVersion)
            return new SnapshotReplayContentResult.NotFound();

        var resource = manifest.Resources.SingleOrDefault(item =>
            string.Equals(item.Path, safePath, StringComparison.Ordinal));
        if (resource is null || !resource.ObjectKey.StartsWith(snapshot.ContentPrefix + '/', StringComparison.Ordinal) ||
            resource.ObjectKey.Contains("..", StringComparison.Ordinal))
            return new SnapshotReplayContentResult.NotFound();

        var stored = await objectStore.GetAsync(resource.ObjectKey, cancellationToken);
        return stored is null
            ? new SnapshotReplayContentResult.NotFound()
            : new SnapshotReplayContentResult.Found(new(stored.Content, resource.ContentType, resource.Role));
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
