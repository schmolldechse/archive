using System.Security.Cryptography;
using System.Text.Json;
using Archive.Core.Authorization;
using Archive.Core.Entities;
using Archive.Core.Entities.Authentication;
using Archive.Core.Models.Snapshots.History;
using Archive.Core.Models.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.SnapshotSubmissionRepository;

internal sealed partial class SnapshotSubmissionRepository
{
    /// <summary>Changes only reviewable snapshots and appends history in the same transaction.</summary>
    public async Task<SnapshotReviewOutcome> ReviewAsync(Guid actorId, Guid id, string expectedTag,
        SnapshotModerationStatus decision, string? reason, CancellationToken token)
    {
        if (decision is not (SnapshotModerationStatus.Approved or SnapshotModerationStatus.Rejected)
            || reason?.Length > 2048) return SnapshotReviewOutcome.Conflict;
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var actor = await LockActorAsync(actorId, token);
        if (!PermissionEvaluator.IsAllowed(actor, ArchivePermissions.AccessAdminPortal)
            || !PermissionEvaluator.IsAllowed(actor, ArchivePermissions.AccessAdminSnapshots)) return SnapshotReviewOutcome.Forbidden;
        var snapshot = await LockSnapshotAsync(id, token);
        if (snapshot is null) return SnapshotReviewOutcome.NotFound;
        var sequence = await CurrentSequenceAsync(snapshot.Id, token);
        if (expectedTag != Tag(snapshot, sequence, true)) return SnapshotReviewOutcome.Stale;
        if (snapshot.Quality is null || snapshot.ModerationStatus is not (SnapshotModerationStatus.Pending or SnapshotModerationStatus.Resubmitted))
            return SnapshotReviewOutcome.Conflict;
        var previous = snapshot.ModerationStatus;
        snapshot.ModerationStatus = decision;
        await history.AppendAsync(snapshot,
            new SnapshotModeratedData(previous, snapshot.ModerationStatus,
                string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()), actorUserId: actorId,
            cancellationToken: token);
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return SnapshotReviewOutcome.Updated;
    }

    /// <summary>Requests another review without changing captured content, metadata or identity.</summary>
    public async Task<SnapshotReviewOutcome> ResubmitAsync(Guid userId, Guid id, string expectedTag, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        if (await LockActorAsync(userId, token) is null) return SnapshotReviewOutcome.Forbidden;
        var snapshot = await LockSnapshotAsync(id, token);
        if (snapshot is null || snapshot.CreatedByUserId != userId) return SnapshotReviewOutcome.NotFound;
        var sequence = await CurrentSequenceAsync(snapshot.Id, token);
        if (expectedTag != Tag(snapshot, sequence, false)) return SnapshotReviewOutcome.Stale;
        if (snapshot.ModerationStatus != SnapshotModerationStatus.Rejected || snapshot.Quality is null)
            return SnapshotReviewOutcome.Conflict;
        snapshot.ModerationStatus = SnapshotModerationStatus.Resubmitted;
        await history.AppendAsync(snapshot,
            new SnapshotResubmittedData(SnapshotModerationStatus.Rejected, snapshot.ModerationStatus),
            actorUserId: userId, cancellationToken: token);
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return SnapshotReviewOutcome.Updated;
    }

    // Access writes lock this same user row before changing role or overrides.
    private Task<ArchiveUser?> LockActorAsync(Guid id, CancellationToken token) => database.Users
        .FromSqlInterpolated($"SELECT * FROM authentication.users WHERE id = {id} FOR SHARE")
        .Include(user => user.PermissionOverrides).SingleOrDefaultAsync(token);

    private Task<Snapshot?> LockSnapshotAsync(Guid id, CancellationToken token) => database.Snapshots
        .FromSqlInterpolated($"SELECT * FROM archive.snapshots WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(token);

    private async Task<long> CurrentSequenceAsync(Guid snapshotId, CancellationToken token) =>
        await database.SnapshotHistory.Where(entry => entry.SnapshotId == snapshotId)
            .MaxAsync(entry => (long?)entry.Sequence, token) ?? 0;

    /// <summary>Opaque conditional-write validator scoped to the owner or moderation representation.</summary>
    private static string Tag(Snapshot snapshot, long sequence, bool review) => "\"" + Convert.ToHexStringLower(
        SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            snapshot.Id,
            Sequence = sequence,
            snapshot.ModerationStatus,
            snapshot.Quality,
            snapshot.CaptureCompletedAt,
            snapshot.ContentPrefix,
            Review = review
        }))) + "\"";
}
