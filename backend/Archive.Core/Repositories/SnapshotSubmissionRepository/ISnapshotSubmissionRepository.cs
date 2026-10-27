using Archive.Core.Entities;
using Archive.Core.Models.Snapshots;
using Archive.Core.Storage;

namespace Archive.Core.Repositories.SnapshotSubmissionRepository;

/// <summary>Private submission reads and transactional decisions with explicit ownership boundaries.</summary>
public interface ISnapshotSubmissionRepository
{
    Task<SnapshotSubmissionPage> SearchOwnAsync(Guid userId, string? text, SnapshotModerationStatus? status, int page, int pageSize, CancellationToken token);
    Task<SnapshotSubmissionPage> SearchForAdminAsync(string? text, SnapshotModerationStatus? status, SnapshotQuality? quality, int page, int pageSize, CancellationToken token);
    Task<SnapshotSubmissionDetail?> GetOwnAsync(Guid userId, Guid id, CancellationToken token);
    Task<SnapshotSubmissionDetail?> GetForReviewAsync(Guid id, CancellationToken token);
    Task<SnapshotHistoryPage?> HistoryOwnAsync(Guid userId, Guid id, int page, int pageSize, CancellationToken token);
    Task<SnapshotHistoryPage?> HistoryForReviewAsync(Guid id, int page, int pageSize, CancellationToken token);
    Task<ArchiveObject?> ScreenshotOwnAsync(Guid userId, Guid id, CancellationToken token);
    Task<ArchiveObject?> ScreenshotForReviewAsync(Guid id, CancellationToken token);
    Task<SnapshotReviewOutcome> ReviewAsync(Guid actorId, Guid id, string expectedTag, SnapshotModerationStatus decision, string? reason, CancellationToken token);
    Task<SnapshotReviewOutcome> ResubmitAsync(Guid userId, Guid id, string expectedTag, CancellationToken token);
    Task<SnapshotUpdateResult> UpdateOwnAsync(Guid userId, Guid id, string expectedTag, SnapshotUpdate update,
        CancellationToken token);
    Task<SnapshotUpdateResult> UpdateForAdminAsync(Guid actorId, Guid id, string expectedTag,
        SnapshotAdministrationUpdate update, CancellationToken token);
    Task<SnapshotDeletionResult> DeleteOwnAsync(Guid userId, Guid id, string expectedTag,
        CancellationToken token);
    Task<SnapshotDeletionResult> DeleteForAdminAsync(Guid actorId, Guid id, string expectedTag,
        CancellationToken token);
}
