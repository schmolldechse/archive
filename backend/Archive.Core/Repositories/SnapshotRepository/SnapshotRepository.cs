using System.Text;
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
    ILogger<SnapshotRepository> logger) : ISnapshotRepository
{
    public async Task<SnapshotDetails?> GetByIdAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        return await database.Snapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId
                && snapshot.ModerationStatus == SnapshotModerationStatus.Approved
                && !snapshot.IsPrivate
                && snapshot.Quality != null)
            .ProjectToDetails()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<SnapshotHistoryPage?> HistoryAsync(Guid snapshotId, int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        history.ReadPublicAsync(snapshotId, page, pageSize, cancellationToken);

    public Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path,
        SnapshotContentRequest? request = null,
        CancellationToken cancellationToken = default) =>
        OpenContentAsync(snapshotId, path, null, requireApproved: true, request, cancellationToken);

    public Task<SnapshotContentResult> OpenAuthorizedContentAsync(Guid snapshotId, string? path, Guid subjectUserId,
        SnapshotContentRequest? request = null,
        CancellationToken cancellationToken = default) =>
        OpenContentAsync(snapshotId, path, subjectUserId, requireApproved: false, request, cancellationToken);

    private async Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path, Guid? subjectUserId,
        bool requireApproved, SnapshotContentRequest? request, CancellationToken cancellationToken)
    {
        var snapshot = await database.Snapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId
                && snapshot.Quality != null)
            .Select(snapshot => new
            {
                snapshot.ContentPrefix,
                snapshot.OriginUrl,
                snapshot.CreatedByUserId,
                snapshot.ModerationStatus,
                snapshot.IsPrivate
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (snapshot?.ContentPrefix is null || requireApproved &&
            (snapshot.ModerationStatus != SnapshotModerationStatus.Approved || snapshot.IsPrivate))
        {
            logger.LogWarning("Snapshot {SnapshotId} is not public or has no active content generation", snapshotId);
            return new SnapshotContentResult.NotFound();
        }

        if (!requireApproved &&
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

        var storedManifest = await objectStore.GetAsync($"{snapshot.ContentPrefix}/manifest.json", cancellationToken);
        if (storedManifest is null)
        {
            logger.LogWarning("Snapshot {SnapshotId} has no manifest in active generation {ContentPrefix}",
                snapshotId, snapshot.ContentPrefix);
            return new SnapshotContentResult.NotFound();
        }

        ResolvedSnapshotManifest manifest;
        try
        {
            await using (storedManifest.Content)
                manifest = await SnapshotManifestReader.ReadAsync(storedManifest.Content, snapshot.ContentPrefix,
                    snapshot.OriginUrl, cancellationToken);
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException or InvalidDataException)
        {
            logger.LogWarning("Snapshot {SnapshotId} has an unsupported or invalid manifest in active generation " +
                "{ContentPrefix}: {ManifestError}", snapshotId, snapshot.ContentPrefix, exception.Message);
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

        request ??= SnapshotContentRequest.Full;
        var entityTag = $"\"{resource.Sha256}\"";
        var isDocument = resource.Role is SnapshotResourceRoles.RootDocument or SnapshotResourceRoles.NestedDocument;
        if (!isDocument && !string.IsNullOrWhiteSpace(request.Range) &&
            (string.IsNullOrWhiteSpace(request.IfRange) ||
                string.Equals(request.IfRange.Trim(), entityTag, StringComparison.Ordinal)))
        {
            var metadata = await objectStore.GetMetadataAsync(resource.ObjectKey, cancellationToken);
            if (metadata is null)
                return new SnapshotContentResult.NotFound();
            if (!SnapshotByteRange.TryResolve(request.Range, metadata.Length, out var selected))
                return new SnapshotContentResult.RangeNotSatisfiable(metadata.Length, entityTag);
            var partial = await objectStore.GetRangeAsync(resource.ObjectKey, selected.Start, selected.Length,
                cancellationToken);
            if (partial is null)
                return new SnapshotContentResult.NotFound();
            return new SnapshotContentResult.Found(new(partial.Content, resource.ContentType, false, resource.Role,
                selected.Length, metadata.Length, selected.Start, entityTag, SupportsRanges: true));
        }

        var stored = await objectStore.GetAsync(resource.ObjectKey, cancellationToken);
        if (stored is null)
            logger.LogWarning("Snapshot {SnapshotId} manifest resource {Path} is missing from object storage",
                snapshotId, safePath);
        if (stored is null)
            return new SnapshotContentResult.NotFound();

        var requiresNormalization = isDocument && manifest.Interaction.RequiresNormalization;
        if (!requiresNormalization)
            return new SnapshotContentResult.Found(new(stored.Content, resource.ContentType, false, resource.Role,
                stored.Length, stored.Length, EntityTag: entityTag, SupportsRanges: !isDocument));

        if (!manifest.Interaction.CanNormalize)
        {
            await stored.Content.DisposeAsync();
            logger.LogWarning("Snapshot {SnapshotId} uses unsupported replay normalizer {Normalizer}",
                snapshotId, manifest.Interaction.Normalizer ?? "legacy-unversioned");
            return new SnapshotContentResult.NotFound();
        }

        try
        {
            string html;
            await using (stored.Content)
            using (var reader = new StreamReader(stored.Content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                html = await reader.ReadToEndAsync(cancellationToken);

            var replayMap = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in manifest.Resources)
            {
                if (!string.IsNullOrWhiteSpace(item.SourceUrl))
                    replayMap.TryAdd(item.SourceUrl, item.Path);
            }

            var normalized = SnapshotDocumentNormalizer.Normalize(html,
                resource.SourceUrl ?? manifest.Provenance.SourceUrl, replayMap, resource.Path);
            var bytes = Encoding.UTF8.GetBytes(normalized.Html);
            var normalizedEntityTag = $"\"{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant()}\"";
            return new SnapshotContentResult.Found(new(new MemoryStream(bytes, writable: false),
                resource.ContentType, false, resource.Role, bytes.LongLength, bytes.LongLength,
                EntityTag: normalizedEntityTag));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception,
                "Snapshot {SnapshotId} document {Path} could not be upgraded to replay normalizer {Normalizer}",
                snapshotId, resource.Path, SnapshotDocumentNormalizer.Version);
            return new SnapshotContentResult.NotFound();
        }
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
                && !snapshot.IsPrivate
                && snapshot.Quality != null)
            .Select(snapshot => snapshot.ContentPrefix)
            .SingleOrDefaultAsync(cancellationToken);
        return prefix is null ? null : await objectStore.GetAsync($"{prefix}/screenshot.png", cancellationToken);
    }

    public Task<bool> CanReadProcessingAsync(Guid snapshotId, Guid? subjectUserId,
        CancellationToken cancellationToken = default) =>
        CanAccessProcessingAsync(snapshotId, subjectUserId, allowPrivilegedReader: true, cancellationToken);

    public Task<bool> CanCancelProcessingAsync(Guid snapshotId, Guid? subjectUserId,
        CancellationToken cancellationToken = default) =>
        CanAccessProcessingAsync(snapshotId, subjectUserId, allowPrivilegedReader: false, cancellationToken);

    private async Task<bool> CanAccessProcessingAsync(Guid snapshotId, Guid? subjectUserId,
        bool allowPrivilegedReader, CancellationToken cancellationToken)
    {
        var snapshot = await database.Snapshots.AsNoTracking()
            .Where(item => item.Id == snapshotId)
            .Select(item => new { item.IsPrivate, item.CreatedByUserId })
            .SingleOrDefaultAsync(cancellationToken);
        if (snapshot is null) return false;
        if (!snapshot.IsPrivate) return true;
        if (subjectUserId is null) return false;

        var subject = await database.Users.AsNoTracking()
            .Include(user => user.PermissionOverrides)
            .SingleOrDefaultAsync(user => user.Id == subjectUserId.Value, cancellationToken);
        if (subject is not { IsRegistrationComplete: true } || !Enum.IsDefined(subject.Role)) return false;
        if (snapshot.CreatedByUserId == subject.Id) return true;
        return allowPrivilegedReader &&
            PermissionEvaluator.IsAllowed(subject, ArchivePermissions.AccessAdminPortal) &&
            PermissionEvaluator.IsAllowed(subject, ArchivePermissions.AccessAdminSnapshots);
    }

}
