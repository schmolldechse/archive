using Archive.Core.Models.Snapshots;
using Archive.Core.Entities;
using Archive.Core.Mapping;
using Archive.Core.Storage;
using Archive.Core.Uploads;
using Archive.Core.Options;
using Microsoft.EntityFrameworkCore;
using Archive.Core.Repositories.SnapshotHistoryRepository;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Archive.Core.Repositories.SnapshotRepository;

internal sealed partial class SnapshotRepository(
    DataContext database,
    IArchiveObjectStore objectStore,
    UploadFormatSelector uploadFormatSelector,
    ArchiveOptions options,
    ISnapshotHistoryRepository history,
    ILogger<SnapshotRepository> logger) : ISnapshotRepository
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

    public async Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await database.Snapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId
                && snapshot.ModerationStatus == SnapshotModerationStatus.Approved
                && snapshot.Quality != null)
            .Select(snapshot => new { snapshot.ContentPrefix, snapshot.ReplayVersion })
            .SingleOrDefaultAsync(cancellationToken);
        if (snapshot?.ContentPrefix is null)
        {
            logger.LogWarning("Snapshot {SnapshotId} is not public or has no active content generation", snapshotId);
            return new SnapshotContentResult.NotFound();
        }

        if (!SnapshotContentStorage.TryNormalizePath(path, out var safePath))
        {
            logger.LogWarning("Snapshot {SnapshotId} received invalid content path {Path}", snapshotId, path);
            return new SnapshotContentResult.InvalidPath();
        }

        var storedManifest = await objectStore.GetAsync($"{snapshot.ContentPrefix}/manifest.json", cancellationToken);
        if (storedManifest is null)
        {
            logger.LogWarning("Snapshot {SnapshotId} has no manifest in active generation {ContentPrefix}",
                snapshotId, snapshot.ContentPrefix);
            return new SnapshotContentResult.NotFound();
        }

        SnapshotReplayManifest? manifest;
        try
        {
            await using (storedManifest.Content)
                manifest = await JsonSerializer.DeserializeAsync<SnapshotReplayManifest>(storedManifest.Content,
                    cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            logger.LogWarning("Snapshot {SnapshotId} has an unreadable manifest in active generation {ContentPrefix}",
                snapshotId, snapshot.ContentPrefix);
            return new SnapshotContentResult.NotFound();
        }

        if (manifest is null || manifest.Version != SnapshotReplayManifest.CurrentManifestVersion ||
            manifest.Replay.Version != snapshot.ReplayVersion)
        {
            logger.LogWarning("Snapshot {SnapshotId} manifest contract does not match its active database metadata",
                snapshotId);
            return new SnapshotContentResult.NotFound();
        }

        var resources = manifest.Resources.Where(item =>
            string.Equals(item.Path, safePath, StringComparison.Ordinal)).Take(2).ToArray();
        if (resources.Length != 1)
        {
            logger.LogWarning("Snapshot {SnapshotId} manifest contains {ResourceCount} entries for path {Path}",
                snapshotId, resources.Length, safePath);
            return new SnapshotContentResult.NotFound();
        }
        var resource = resources[0];
        if (!string.Equals(resource.ObjectKey, $"{snapshot.ContentPrefix}/{safePath}", StringComparison.Ordinal))
        {
            logger.LogWarning("Snapshot {SnapshotId} manifest maps path {Path} outside its active generation",
                snapshotId, safePath);
            return new SnapshotContentResult.NotFound();
        }

        var stored = await objectStore.GetAsync(resource.ObjectKey, cancellationToken);
        if (stored is null)
            logger.LogWarning("Snapshot {SnapshotId} manifest resource {Path} is missing from object storage",
                snapshotId, safePath);
        return stored is null
            ? new SnapshotContentResult.NotFound()
            : new SnapshotContentResult.Found(new(stored.Content, resource.ContentType, false,
                resource.Role, snapshot.ReplayVersion));
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
