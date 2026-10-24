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

    public Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path,
        CancellationToken cancellationToken = default) =>
        OpenContentAsync(snapshotId, path, requireApproved: true, cancellationToken);

    public Task<SnapshotContentResult> OpenAuthorizedContentAsync(Guid snapshotId, string? path,
        CancellationToken cancellationToken = default) =>
        OpenContentAsync(snapshotId, path, requireApproved: false, cancellationToken);

    private async Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path, bool requireApproved,
        CancellationToken cancellationToken)
    {
        var snapshot = await database.Snapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId
                && (!requireApproved || snapshot.ModerationStatus == SnapshotModerationStatus.Approved)
                && snapshot.Quality != null)
            .Select(snapshot => new { snapshot.ContentPrefix })
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

        SnapshotManifest? manifest;
        try
        {
            await using (storedManifest.Content)
                manifest = await JsonSerializer.DeserializeAsync<SnapshotManifest>(storedManifest.Content,
                    cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            logger.LogWarning("Snapshot {SnapshotId} has an unreadable manifest in active generation {ContentPrefix}",
                snapshotId, snapshot.ContentPrefix);
            return new SnapshotContentResult.NotFound();
        }

        if (manifest is null || manifest.Version != SnapshotManifest.CurrentVersion)
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
            : new SnapshotContentResult.Found(new(stored.Content, resource.ContentType, false, resource.Role));
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
