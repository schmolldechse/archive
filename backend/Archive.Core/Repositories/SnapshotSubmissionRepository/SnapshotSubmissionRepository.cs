using System.Text.Json;
using Archive.Core.Entities;
using Archive.Core.Entities.Processing;
using Archive.Core.Mapping;
using Archive.Core.Models.Snapshots;
using Archive.Core.Models.Snapshots.History;
using Archive.Core.Repositories.SnapshotHistoryRepository;
using Archive.Core.Serialization;
using Archive.Core.Storage;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.SnapshotSubmissionRepository;

/// <summary>Reads private records without relaxing the public repository's visibility rule.</summary>
internal sealed partial class SnapshotSubmissionRepository(DataContext database, IArchiveObjectStore objectStore,
    ISnapshotHistoryRepository history, SnapshotManifestReader manifestReader) : ISnapshotSubmissionRepository
{
    private static readonly string[] ModerationEventTypes =
        [SnapshotHistoryTypes.Submitted, SnapshotHistoryTypes.Moderated, SnapshotHistoryTypes.Resubmitted,
            SnapshotHistoryTypes.SourceUpdated];

    public Task<SnapshotSubmissionPage> SearchOwnAsync(Guid userId, string? text, SnapshotModerationStatus? status,
        int page, int pageSize, CancellationToken token) => SearchAsync(userId, text, status, null, page, pageSize, token);

    /// <summary>Returns every snapshot record for the permission-checked administration register.</summary>
    public Task<SnapshotSubmissionPage> SearchForAdminAsync(string? text, SnapshotModerationStatus? status,
        SnapshotQuality? quality, int page, int pageSize, CancellationToken token) =>
        SearchAsync(null, text, status, quality, page, pageSize, token);

    private async Task<SnapshotSubmissionPage> SearchAsync(Guid? ownerId, string? text, SnapshotModerationStatus? status,
        SnapshotQuality? quality, int page, int pageSize, CancellationToken token)
    {
        var query = Scope(ownerId);
        if (status is not null) query = query.Where(snapshot => snapshot.ModerationStatus == status);
        if (quality == SnapshotQuality.Complete)
            query = query.Where(snapshot => snapshot.Quality == SnapshotQuality.Complete);
        else if (quality == SnapshotQuality.Incomplete)
            query = query.Where(snapshot => snapshot.Quality != SnapshotQuality.Complete);
        if (!string.IsNullOrWhiteSpace(text))
        {
            var pattern = "%" + text.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(snapshot => EF.Functions.ILike(snapshot.Title, pattern, "\\") ||
                EF.Functions.ILike(snapshot.OriginUrl ?? "", pattern, "\\"));
        }
        var total = await query.CountAsync(token);
        var projected = query
            // Page on entity columns before constructing the correlated projection. EF cannot translate ordering
            // through the nested SnapshotSubmissionRow record, even though the requested key is still CreatedAt.
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .ThenByDescending(snapshot => snapshot.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(snapshot => new SnapshotSubmissionRow(
                snapshot,
                database.SnapshotHistory.Where(entry => entry.SnapshotId == snapshot.Id
                    && ModerationEventTypes.Contains(entry.Content.GetProperty("type").GetString()!))
                    .OrderByDescending(entry => entry.Sequence)
                    .Select(entry => (DateTimeOffset?)entry.CreatedAt)
                    .FirstOrDefault() ?? snapshot.CreatedAt,
                snapshot.Quality != null
                    ? ProcessingStatus.Completed
                    : database.SnapshotProcessing.Where(processing => processing.SnapshotId == snapshot.Id)
                        .Select(processing => (ProcessingStatus?)processing.Status)
                        .FirstOrDefault() ?? ProcessingStatus.Failed));
        var items = await projected.ProjectToSubmissionSummaries().ToArrayAsync(token);
        return SnapshotModelMapper.ToSubmissionPage(items, total, page, pageSize);
    }

    public Task<SnapshotSubmissionDetail?> GetOwnAsync(Guid userId, Guid id, CancellationToken token) => GetAsync(id, userId, token);
    public Task<SnapshotSubmissionDetail?> GetForReviewAsync(Guid id, CancellationToken token) => GetAsync(id, null, token);

    private async Task<SnapshotSubmissionDetail?> GetAsync(Guid id, Guid? ownerId, CancellationToken token)
    {
        var snapshot = await Scope(ownerId).Include(s => s.CreatedBy).Include(s => s.SnapshotTags).ThenInclude(t => t.Tag)
            .SingleOrDefaultAsync(s => s.Id == id, token);
        if (snapshot is null) return null;
        var sequence = await database.SnapshotHistory.AsNoTracking()
            .Where(entry => entry.SnapshotId == id)
            .MaxAsync(entry => (long?)entry.Sequence, token) ?? 0;
        var moderationUpdatedAt = await database.SnapshotHistory.AsNoTracking()
            .Where(entry => entry.SnapshotId == id
                && ModerationEventTypes.Contains(entry.Content.GetProperty("type").GetString()!))
            .OrderByDescending(entry => entry.Sequence)
            .Select(entry => (DateTimeOffset?)entry.CreatedAt)
            .FirstOrDefaultAsync(token) ?? snapshot.CreatedAt;
        var captureStatus = snapshot.Quality is not null
            ? ProcessingStatus.Completed
            : await database.SnapshotProcessing.Where(processing => processing.SnapshotId == id)
                .Select(processing => (ProcessingStatus?)processing.Status).SingleOrDefaultAsync(token)
                ?? ProcessingStatus.Failed;
        var decision = await database.SnapshotHistory.AsNoTracking().Where(entry => entry.SnapshotId == id
                && entry.Content.GetProperty("type").GetString() == SnapshotHistoryTypes.Moderated)
            .OrderByDescending(entry => entry.Sequence).Select(entry => (JsonElement?)entry.Content).FirstOrDefaultAsync(token);
        var reason = decision is { } content
            ? SnapshotHistorySerializer.Deserialize<SnapshotModeratedData>(content).Reason
            : null;
        var moderation = SnapshotModelMapper.ToModerationDetails(snapshot.ModerationStatus, moderationUpdatedAt,
            reason);
        return SnapshotModelMapper.ToSubmissionDetail(SnapshotModelMapper.MapDetails(snapshot), moderation,
            captureStatus, Tag(snapshot, sequence, ownerId is null));
    }

    private IQueryable<Snapshot> Scope(Guid? ownerId) => database.Snapshots.AsNoTracking()
        .Where(snapshot => ownerId == null || snapshot.CreatedByUserId == ownerId);

    public Task<ArchiveObject?> ScreenshotOwnAsync(Guid userId, Guid id, CancellationToken token) => ScreenshotAsync(id, userId, token);
    public Task<ArchiveObject?> ScreenshotForReviewAsync(Guid id, CancellationToken token) => ScreenshotAsync(id, null, token);

    public Task<SnapshotPageTree?> PageTreeOwnAsync(Guid userId, Guid id, CancellationToken token) =>
        PageTreeAsync(id, userId, token);

    public Task<SnapshotPageTree?> PageTreeForReviewAsync(Guid id, CancellationToken token) =>
        PageTreeAsync(id, null, token);

    private async Task<SnapshotPageTree?> PageTreeAsync(Guid id, Guid? ownerId, CancellationToken token)
    {
        var prefix = await Scope(ownerId).Where(snapshot => snapshot.Id == id && snapshot.Quality != null)
            .Select(snapshot => snapshot.ContentPrefix).SingleOrDefaultAsync(token);
        if (prefix is null)
            return null;

        var manifest = await manifestReader.ReadAsync(prefix, token);
        return manifest is not null && SnapshotManifestReader.TryGetPageTree(manifest, out var pageTree)
            ? pageTree
            : null;
    }

    private async Task<ArchiveObject?> ScreenshotAsync(Guid id, Guid? ownerId, CancellationToken token)
    {
        var prefix = await Scope(ownerId).Where(snapshot => snapshot.Id == id && snapshot.Quality != null)
            .Select(snapshot => snapshot.ContentPrefix).SingleOrDefaultAsync(token);
        return prefix is null ? null : await objectStore.GetAsync($"{prefix}/screenshot.png", token);
    }

    public Task<SnapshotHistoryPage?> HistoryOwnAsync(Guid userId, Guid id, int page, int pageSize, CancellationToken token) =>
        history.ReadOwnAsync(userId, id, page, pageSize, token);

    public Task<SnapshotHistoryPage?> HistoryForReviewAsync(Guid id, int page, int pageSize, CancellationToken token) =>
        history.ReadAdminAsync(id, page, pageSize, token);

}
