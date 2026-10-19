using System.Text.Json;
using Archive.Core.Entities;
using Archive.Core.Entities.Processing;
using Archive.Core.Mapping;
using Archive.Core.Models.Snapshots;
using Archive.Core.Models.Snapshots.History;
using Archive.Core.Models.Users;
using Archive.Core.Repositories.SnapshotHistoryRepository;
using Archive.Core.Serialization;
using Archive.Core.Storage;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.SnapshotSubmissionRepository;

/// <summary>Reads private records without relaxing the public repository's visibility rule.</summary>
internal sealed partial class SnapshotSubmissionRepository(DataContext database, IArchiveObjectStore objectStore,
    ISnapshotHistoryRepository history) : ISnapshotSubmissionRepository
{
    private static readonly string[] ModerationEventTypes =
        ["snapshot.submitted", "snapshot.moderated", "snapshot.resubmitted"];

    public Task<SnapshotSubmissionPage> SearchOwnAsync(Guid userId, string? text, SnapshotModerationStatus? status,
        int page, int pageSize, CancellationToken token) => SearchAsync(userId, text, status, null, false, page, pageSize, token);

    /// <summary>Returns every snapshot record for the permission-checked administration register.</summary>
    public Task<SnapshotSubmissionPage> SearchForAdminAsync(string? text, SnapshotModerationStatus? status,
        SnapshotQuality? quality, int page, int pageSize, CancellationToken token) =>
        SearchAsync(null, text, status, quality, false, page, pageSize, token);

    /// <summary>Returns only captured snapshots that still need a first or renewed decision.</summary>
    public Task<SnapshotSubmissionPage> SearchForReviewAsync(string? text, SnapshotModerationStatus? status,
        int page, int pageSize, CancellationToken token) =>
        SearchAsync(null, text, status, null, true, page, pageSize, token);

    private async Task<SnapshotSubmissionPage> SearchAsync(Guid? ownerId, string? text, SnapshotModerationStatus? status,
        SnapshotQuality? quality, bool reviewQueue, int page, int pageSize, CancellationToken token)
    {
        var query = Scope(ownerId);
        if (reviewQueue)
            query = query.Where(snapshot => snapshot.Quality != null &&
                (snapshot.ModerationStatus == SnapshotModerationStatus.Pending ||
                 snapshot.ModerationStatus == SnapshotModerationStatus.Resubmitted));
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
        var projected = query.Select(snapshot => new
        {
            Snapshot = snapshot,
            UpdatedAt = database.SnapshotHistory.Where(entry => entry.SnapshotId == snapshot.Id
                    && ModerationEventTypes.Contains(entry.Content.GetProperty("type").GetString()!))
                .OrderByDescending(entry => entry.Sequence)
                .Select(entry => (DateTimeOffset?)entry.CreatedAt)
                .FirstOrDefault() ?? snapshot.CreatedAt,
            ProcessingStatus = snapshot.Quality != null
                ? ProcessingStatus.Completed
                : database.SnapshotProcessing.Where(processing => processing.SnapshotId == snapshot.Id)
                    .Select(processing => (ProcessingStatus?)processing.Status)
                    .FirstOrDefault() ?? ProcessingStatus.Failed
        });
        var ordered = reviewQueue
            ? projected.OrderBy(row => row.UpdatedAt).ThenBy(row => row.Snapshot.Id)
            : projected.OrderByDescending(row => row.Snapshot.CreatedAt).ThenByDescending(row => row.Snapshot.Id);
        var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(row =>
            new SnapshotSubmissionSummary(row.Snapshot.Id, row.Snapshot.Title, row.Snapshot.OriginUrl,
                row.Snapshot.SourceType, row.Snapshot.CreatedAt, row.UpdatedAt, row.Snapshot.ModerationStatus,
                row.ProcessingStatus, row.Snapshot.Quality, row.Snapshot.CaptureCompletedAt,
                row.Snapshot.DurationMilliseconds, row.Snapshot.StorageBytes, row.Snapshot.ResourceCount,
                row.Snapshot.CreatedBy == null || row.Snapshot.CreatedBy.Handle == null || row.Snapshot.CreatedBy.DisplayName == null
                    ? null
                    : new PublicUser(row.Snapshot.CreatedBy.Handle, row.Snapshot.CreatedBy.DisplayName,
                        row.Snapshot.CreatedBy.ProfileImage != null && row.Snapshot.CreatedBy.ProfileImage.Length > 0,
                        row.Snapshot.CreatedBy.Role))).ToArrayAsync(token);
        return new(items, total, page, pageSize);
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
                && entry.Content.GetProperty("type").GetString() == "snapshot.moderated")
            .OrderByDescending(entry => entry.Sequence).Select(entry => (JsonElement?)entry.Content).FirstOrDefaultAsync(token);
        var reason = decision is { } content
            ? SnapshotHistorySerializer.Deserialize<SnapshotModeratedData>(content).Reason
            : null;
        return new(SnapshotModelMapper.MapDetails(snapshot),
            new(snapshot.ModerationStatus, moderationUpdatedAt, reason), captureStatus,
            Tag(snapshot, sequence, ownerId is null));
    }

    private IQueryable<Snapshot> Scope(Guid? ownerId) => database.Snapshots.AsNoTracking()
        .Where(snapshot => ownerId == null || snapshot.CreatedByUserId == ownerId);

    public Task<ArchiveObject?> ScreenshotOwnAsync(Guid userId, Guid id, CancellationToken token) => ScreenshotAsync(id, userId, token);
    public Task<ArchiveObject?> ScreenshotForReviewAsync(Guid id, CancellationToken token) => ScreenshotAsync(id, null, token);

    private async Task<ArchiveObject?> ScreenshotAsync(Guid id, Guid? ownerId, CancellationToken token)
    {
        var prefix = await Scope(ownerId).Where(snapshot => snapshot.Id == id && snapshot.Quality != null)
            .Select(snapshot => snapshot.ContentPrefix).SingleOrDefaultAsync(token);
        return prefix is null ? null : await objectStore.GetAsync($"{prefix}/screenshot.png", token);
    }

    public Task<SnapshotContentResult> OpenContentOwnAsync(Guid userId, Guid id, string? path, CancellationToken token) =>
        OpenContentAsync(id, userId, path, token);

    public Task<SnapshotContentResult> OpenContentForReviewAsync(Guid id, string? path, CancellationToken token) =>
        OpenContentAsync(id, null, path, token);

    private async Task<SnapshotContentResult> OpenContentAsync(Guid id, Guid? ownerId, string? path, CancellationToken token)
    {
        var snapshot = await Scope(ownerId).Where(item => item.Id == id && item.Quality != null)
            .Select(item => new { item.ContentPrefix, item.SourceType }).SingleOrDefaultAsync(token);
        if (snapshot?.ContentPrefix is null) return new SnapshotContentResult.NotFound();

        return await objectStore.OpenSnapshotContentAsync(snapshot.ContentPrefix, snapshot.SourceType, path, token);
    }

    public Task<SnapshotHistoryPage?> HistoryOwnAsync(Guid userId, Guid id, int page, int pageSize, CancellationToken token) =>
        HistoryAsync(id, userId, page, pageSize, token);

    public Task<SnapshotHistoryPage?> HistoryForReviewAsync(Guid id, int page, int pageSize, CancellationToken token) =>
        HistoryAsync(id, null, page, pageSize, token);

    private async Task<SnapshotHistoryPage?> HistoryAsync(Guid id, Guid? ownerId, int page, int pageSize, CancellationToken token)
    {
        if (!await Scope(ownerId).AnyAsync(snapshot => snapshot.Id == id, token)) return null;
        var query = database.SnapshotHistory.AsNoTracking().Where(entry => entry.SnapshotId == id);
        if (ownerId is not null)
            // Future types and versions must be explicitly approved before an owner can read them.
            query = query.Where(entry => entry.Content.GetProperty("version").GetInt32() == 1 &&
                (entry.Content.GetProperty("type").GetString() == "snapshot.submitted" ||
                 entry.Content.GetProperty("type").GetString() == "snapshot.moderated" ||
                 entry.Content.GetProperty("type").GetString() == "snapshot.resubmitted" ||
                 entry.Content.GetProperty("type").GetString() == "snapshot.stored" ||
                 entry.Content.GetProperty("type").GetString() == "snapshot.processing_finished"));
        var total = await query.CountAsync(token);
        var rows = await query.OrderByDescending(entry => entry.Sequence).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(entry => new
            {
                entry.Sequence,
                entry.CreatedAt,
                entry.Content,
                Actor = ownerId == null && entry.Actor != null && entry.Actor.Handle != null && entry.Actor.DisplayName != null
                    ? new PublicUser(entry.Actor.Handle, entry.Actor.DisplayName,
                        entry.Actor.ProfileImage != null && entry.Actor.ProfileImage.Length > 0, entry.Actor.Role)
                    : null
            }).ToArrayAsync(token);
        var items = rows.Select(row => new SnapshotHistoryItem(row.Sequence, row.CreatedAt,
            row.Actor, SnapshotHistorySerializer.Deserialize(row.Content))).ToArray();
        return new(items, total, page, pageSize);
    }

}
