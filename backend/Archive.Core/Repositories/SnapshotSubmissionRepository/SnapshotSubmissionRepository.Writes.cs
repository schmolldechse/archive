using System.Security.Cryptography;
using System.Text.Json;
using Archive.Core.Authorization;
using Archive.Core.Entities;
using Archive.Core.Entities.Authentication;
using Archive.Core.Jobs;
using Archive.Core.Models;
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

    /// <summary>Updates an owned snapshot and requests review only when its stored provenance URL changes.</summary>
    public Task<SnapshotUpdateResult> UpdateOwnAsync(Guid userId, Guid id, string expectedTag,
        SnapshotUpdate update, CancellationToken token) =>
        UpdateAsync(userId, id, expectedTag, update, administrator: false, createdByHandle: null, token);

    /// <summary>Updates any snapshot and its account attribution without changing its moderation status.</summary>
    public Task<SnapshotUpdateResult> UpdateForAdminAsync(Guid actorId, Guid id, string expectedTag,
        SnapshotAdministrationUpdate update, CancellationToken token) =>
        UpdateAsync(actorId, id, expectedTag, update.Snapshot, administrator: true, update.CreatedByHandle, token);

    /// <summary>Serializes field changes, tag links, attribution and history under the snapshot row lock.</summary>
    private async Task<SnapshotUpdateResult> UpdateAsync(Guid actorId, Guid id, string expectedTag,
        SnapshotUpdate update, bool administrator, string? createdByHandle, CancellationToken token)
    {
        var errors = ModelValidation.GetErrors(update);
        if (administrator)
            errors = errors.Concat(ModelValidation.GetErrors(new SnapshotAdministrationUpdate(update, createdByHandle))).ToArray();
        if (errors.Count > 0) return new SnapshotUpdateResult.Invalid(errors);
        if (!UrlNormalizer.TryNormalizeSource(update.SourceUrl, out var sourceUrl, out _))
            return new SnapshotUpdateResult.Invalid([new(nameof(SnapshotUpdate.SourceUrl), "Enter a valid HTTP or HTTPS URL.")]);

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var actor = await LockActorAsync(actorId, token);
        if (actor is null) return new SnapshotUpdateResult.Forbidden();
        if (administrator && (!PermissionEvaluator.IsAllowed(actor, ArchivePermissions.AccessAdminPortal) ||
            !PermissionEvaluator.IsAllowed(actor, ArchivePermissions.AccessAdminSnapshots)))
            return new SnapshotUpdateResult.Forbidden();

        var snapshot = await LockSnapshotAsync(id, token);
        if (snapshot is null || !administrator && snapshot.CreatedByUserId != actorId)
            return new SnapshotUpdateResult.NotFound();
        var sequence = await CurrentSequenceAsync(snapshot.Id, token);
        if (expectedTag != Tag(snapshot, sequence, administrator)) return new SnapshotUpdateResult.Stale();

        ArchiveUser? targetOwner = null;
        if (administrator && createdByHandle is not null)
        {
            targetOwner = await database.Users
                .FromSqlInterpolated($"SELECT * FROM authentication.users WHERE handle = {createdByHandle} FOR SHARE")
                .SingleOrDefaultAsync(token);
            if (targetOwner?.RegistrationCompletedAt is null)
                return new SnapshotUpdateResult.Invalid([
                    new(nameof(SnapshotAdministrationUpdate.CreatedByHandle), "Choose an existing registered user.")
                ]);
        }

        await database.Entry(snapshot).Collection(item => item.SnapshotTags).Query()
            .Include(item => item.Tag).LoadAsync(token);
        var previousTags = snapshot.SnapshotTags.Select(item => item.Tag.Value)
            .Order(StringComparer.Ordinal).ToArray();
        var tags = update.Tags.Order(StringComparer.Ordinal).ToArray();
        var titleChanged = !string.Equals(snapshot.Title, update.Title, StringComparison.Ordinal);
        var descriptionChanged = !string.Equals(snapshot.Description, update.Description, StringComparison.Ordinal);
        var tagsChanged = !previousTags.SequenceEqual(tags, StringComparer.Ordinal);
        var sourceChanged = !string.Equals(snapshot.OriginUrl, sourceUrl, StringComparison.Ordinal);
        var ownerChanged = administrator && snapshot.CreatedByUserId != targetOwner?.Id;

        if (sourceChanged && snapshot.Quality is null) return new SnapshotUpdateResult.SourceNotEditable();
        if (!titleChanged && !descriptionChanged && !tagsChanged && !sourceChanged && !ownerChanged)
            return new SnapshotUpdateResult.Unchanged();

        var previousTitle = snapshot.Title;
        var previousDescription = snapshot.Description;
        var previousSourceUrl = snapshot.OriginUrl;
        var previousStatus = snapshot.ModerationStatus;
        var previousUserId = snapshot.CreatedByUserId;
        snapshot.Title = update.Title;
        snapshot.Description = update.Description;
        snapshot.OriginUrl = sourceUrl;
        if (ownerChanged) snapshot.CreatedByUserId = targetOwner?.Id;

        if (tagsChanged)
        {
            var desiredTags = tags.ToHashSet(StringComparer.Ordinal);
            var removedLinks = snapshot.SnapshotTags
                .Where(link => !desiredTags.Contains(link.Tag.Value)).ToArray();
            database.SnapshotTags.RemoveRange(removedLinks);
            foreach (var link in removedLinks)
                snapshot.SnapshotTags.Remove(link);

            // Preserve unchanged links so EF never tracks a deleted and added entity
            // with the same composite key in one update.
            foreach (var value in tags.Except(previousTags, StringComparer.Ordinal))
            {
                await database.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO archive.tags (id, value) VALUES ({Guid.NewGuid()}, {value})
                    ON CONFLICT (value) DO NOTHING
                    """, token);
                var tagId = await database.Tags.Where(tag => tag.Value == value)
                    .Select(tag => tag.Id).SingleAsync(token);
                snapshot.SnapshotTags.Add(new SnapshotTag { Snapshot = snapshot, TagId = tagId });
            }
        }

        var changedFields = new List<SnapshotDetailField>(3);
        if (titleChanged) changedFields.Add(SnapshotDetailField.Title);
        if (descriptionChanged) changedFields.Add(SnapshotDetailField.Description);
        if (tagsChanged) changedFields.Add(SnapshotDetailField.Tags);
        if (changedFields.Count > 0)
        {
            await history.AppendAsync(snapshot, new SnapshotDetailsUpdatedData(changedFields,
                    tags.Except(previousTags, StringComparer.Ordinal).ToArray(),
                    previousTags.Except(tags, StringComparer.Ordinal).ToArray(),
                    titleChanged ? new SnapshotTextChange(previousTitle, update.Title) : null,
                    descriptionChanged ? new SnapshotTextChange(previousDescription, update.Description) : null,
                    tagsChanged ? new SnapshotTagsChange(previousTags, tags) : null),
                actorUserId: actorId, cancellationToken: token);
        }

        if (sourceChanged)
        {
            if (!administrator) snapshot.ModerationStatus = SnapshotModerationStatus.Resubmitted;
            await history.AppendAsync(snapshot,
                new SnapshotSourceUpdatedData(previousSourceUrl, sourceUrl, previousStatus,
                    snapshot.ModerationStatus, !administrator),
                actorUserId: actorId, cancellationToken: token);
        }

        if (ownerChanged)
        {
            await history.AppendAsync(snapshot,
                new SnapshotAttributionUpdatedData(previousUserId, snapshot.CreatedByUserId),
                actorUserId: actorId, cancellationToken: token);
        }

        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return new SnapshotUpdateResult.Updated();
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
