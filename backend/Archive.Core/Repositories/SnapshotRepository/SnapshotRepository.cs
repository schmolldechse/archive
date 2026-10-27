using Archive.Core.Models.Snapshots;
using Archive.Core.Entities;
using Archive.Core.Mapping;
using Archive.Core.Storage;
using Archive.Core.Uploads;
using Archive.Core.Options;
using Microsoft.EntityFrameworkCore;
using Archive.Core.Repositories.SnapshotHistoryRepository;
using Microsoft.Extensions.Logging;
using Archive.Core.Authorization;

namespace Archive.Core.Repositories.SnapshotRepository;

internal sealed partial class SnapshotRepository(
    DataContext database,
    IArchiveObjectStore objectStore,
    UploadFormatSelector uploadFormatSelector,
    ArchiveOptions options,
    ISnapshotHistoryRepository history,
    SnapshotManifestReader manifestReader,
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

    public async Task<SnapshotPageTree?> GetPageTreeAsync(Guid snapshotId,
        CancellationToken cancellationToken = default)
    {
        var prefix = await database.Snapshots.AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId &&
                snapshot.ModerationStatus == SnapshotModerationStatus.Approved && snapshot.Quality != null)
            .Select(snapshot => snapshot.ContentPrefix)
            .SingleOrDefaultAsync(cancellationToken);
        if (prefix is null)
            return null;

        var manifest = await manifestReader.ReadAsync(prefix, cancellationToken);
        return manifest is not null && SnapshotManifestReader.TryGetPageTree(manifest, out var pageTree)
            ? pageTree
            : null;
    }

    public Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path,
        CancellationToken cancellationToken = default) =>
        OpenContentAsync(snapshotId, path, null, requireApproved: true, cancellationToken);

    public Task<SnapshotContentResult> OpenAuthorizedContentAsync(Guid snapshotId, string? path, Guid subjectUserId,
        CancellationToken cancellationToken = default) =>
        OpenContentAsync(snapshotId, path, subjectUserId, requireApproved: false, cancellationToken);

    private async Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path, Guid? subjectUserId,
        bool requireApproved, CancellationToken cancellationToken)
    {
        var snapshot = await database.Snapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId
                && snapshot.Quality != null)
            .Select(snapshot => new
            {
                snapshot.ContentPrefix,
                snapshot.CreatedByUserId,
                snapshot.ModerationStatus
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (snapshot?.ContentPrefix is null || requireApproved &&
            snapshot.ModerationStatus != SnapshotModerationStatus.Approved)
        {
            logger.LogWarning("Snapshot {SnapshotId} is not public or has no active content generation", snapshotId);
            return new SnapshotContentResult.NotFound();
        }

        if (!requireApproved && snapshot.ModerationStatus != SnapshotModerationStatus.Approved &&
            (subjectUserId is null || !await HasPrivateContentAccessAsync(
                snapshot.CreatedByUserId, subjectUserId.Value, cancellationToken)))
        {
            logger.LogWarning("Snapshot {SnapshotId} private content capability is no longer authorized", snapshotId);
            return new SnapshotContentResult.NotFound();
        }

        if (!SnapshotContentStorage.TryNormalizePath(path, out var safePath))
        {
            logger.LogWarning("Snapshot {SnapshotId} received invalid content path {Path}", snapshotId, path);
            return new SnapshotContentResult.InvalidPath();
        }

        var manifest = await manifestReader.ReadAsync(snapshot.ContentPrefix, cancellationToken);
        if (manifest is null)
        {
            logger.LogWarning("Snapshot {SnapshotId} has no readable current manifest in active generation {ContentPrefix}",
                snapshotId, snapshot.ContentPrefix);
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

    /// <summary>Revalidates owner or moderation access whenever a private content capability is used.</summary>
    private async Task<bool> HasPrivateContentAccessAsync(Guid? ownerId, Guid subjectUserId,
        CancellationToken cancellationToken)
    {
        var subject = await database.Users.AsNoTracking()
            .Include(user => user.PermissionOverrides)
            .SingleOrDefaultAsync(user => user.Id == subjectUserId, cancellationToken);
        if (subject is not { IsRegistrationComplete: true } || !Enum.IsDefined(subject.Role))
            return false;
        if (ownerId == subjectUserId)
            return true;

        return PermissionEvaluator.IsAllowed(subject, ArchivePermissions.AccessAdminPortal) &&
            PermissionEvaluator.IsAllowed(subject, ArchivePermissions.AccessAdminSnapshots);
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
