using Archive.Core.Entities;
using Archive.Core.Authorization;
using Archive.Core.Models.Snapshots.History;
using Archive.Core.Entities.Processing;
using Archive.Core.Jobs;
using Archive.Core.Models;
using Archive.Core.Models.Snapshots;
using Archive.Core.Uploads;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.SnapshotRepository;

internal sealed partial class SnapshotRepository
{
    public Task<SnapshotCreationResult> CreateFromUrlAsync(SnapshotCreationRequest request, CancellationToken cancellationToken = default) =>
        CreateAsync(request, null, cancellationToken);

    public Task<SnapshotCreationResult> CreateFromFileAsync(SnapshotCreationRequest request, SnapshotUpload upload, CancellationToken cancellationToken = default) =>
        CreateAsync(request, upload, cancellationToken);

    private async Task<SnapshotCreationResult> CreateAsync(SnapshotCreationRequest request, SnapshotUpload? upload, CancellationToken cancellationToken)
    {
        var errors = ModelValidation.GetErrors(request);
        if (errors.Count > 0)
            return new SnapshotCreationResult.Invalid(errors);

        UrlNormalizer.TryNormalizeSource(request.SourceUrl, out var originUrl, out _);
        var title = request.Title.Trim();
        var tags = (request.Tags ?? []).Select(SnapshotValidation.NormalizeTag).ToArray();

        IUploadFormatProbe? format = null;
        if (upload is not null)
        {
            if (upload.Length > options.MaxUploadBytes)
                return new SnapshotCreationResult.UploadTooLarge();
            if (upload.Length <= 0)
                return new SnapshotCreationResult.Invalid([new("file", "Provide a non-empty .html, .mhtml or .webarchive file.")]);
            var prefix = new byte[(int)Math.Min(upload.Length, 8_192)];
            await using (var stream = upload.OpenReadStream())
            {
                var read = 0;
                while (read < prefix.Length)
                {
                    var count = await stream.ReadAsync(prefix.AsMemory(read), cancellationToken);
                    if (count == 0) break;
                    read += count;
                }
                format = uploadFormatSelector.Select(upload.FileName, prefix.AsSpan(0, read));
            }

            if (format is null)
                return new SnapshotCreationResult.Invalid([new("file", "The file format is unsupported or does not match its extension.")]);
        }

        var description = request.Description?.Trim();
        var snapshot = new Snapshot
        {
            SourceType = format?.SourceType ?? SourceType.Url,
            OriginUrl = originUrl,
            Title = title,
            Description = string.IsNullOrWhiteSpace(description) ? null : description
        };
        string? uploadedKey = null;
        try
        {
            if (upload is not null)
            {
                uploadedKey = $"jobs/{snapshot.Id:N}/source{format!.Extension}";
                await using var stream = upload.OpenReadStream();
                await objectStore.PutAsync(uploadedKey, stream, format.ContentType, cancellationToken);
            }

            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

            // Coordinate with access-management writes before reading permission overrides.
            var creator = request.CreatedByUserId is { } userId
                ? await database.Users
                    .FromSqlInterpolated($"SELECT * FROM authentication.users WHERE id = {userId} FOR SHARE")
                    .Include(user => user.PermissionOverrides)
                    .SingleOrDefaultAsync(cancellationToken)
                : null;
            if (request.CreatedByUserId is not null && creator is null)
                throw new MissingCreatorException();

            snapshot.CreatedByUserId = creator?.Id;
            snapshot.ModerationStatus = PermissionEvaluator.IsAllowed(creator, ArchivePermissions.PublishSnapshotsWithoutApproval)
                ? SnapshotModerationStatus.Approved
                : SnapshotModerationStatus.Pending;

            database.Snapshots.Add(snapshot);
            await history.AppendAsync(snapshot, new SnapshotSubmittedData(), actorUserId: creator?.Id, cancellationToken: cancellationToken);

            foreach (var value in tags.Distinct(StringComparer.Ordinal))
            {
                await database.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO archive.tags (id, value) VALUES ({Guid.NewGuid()}, {value})
                    ON CONFLICT (value) DO NOTHING
                    """, cancellationToken);
                var tagId = await database.Tags
                    .Where(tag => tag.Value == value)
                    .Select(tag => tag.Id)
                    .SingleAsync(cancellationToken);
                snapshot.SnapshotTags.Add(new SnapshotTag { Snapshot = snapshot, TagId = tagId });
            }

            database.SnapshotProcessing.Add(new SnapshotProcessing
            {
                Snapshot = snapshot,
                // Zero is EF's unset sentinel; PostgreSQL generates the queue sequence on insertion.
                QueueSequence = 0,
                Status = ProcessingStatus.Queued,
                ProgressStep = ProgressStep.SourceCheck,
                UploadedObjectKey = uploadedKey
            });

            if (snapshot.ModerationStatus == SnapshotModerationStatus.Approved)
                await history.AppendAsync(snapshot, new SnapshotModeratedData(null, snapshot.ModerationStatus, null), cancellationToken: cancellationToken);
            
            // Per-snapshot positions include pending entries, so both events are saved atomically.
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            if (uploadedKey is not null)
            {
                try
                {
                    // The transaction has been disposed. Preserve uploads after ambiguous commits.
                    if (!await database.Snapshots.AsNoTracking().AnyAsync(item => item.Id == snapshot.Id, CancellationToken.None))
                        await objectStore.DeleteAsync(uploadedKey, CancellationToken.None);
                }
                catch { /* A later reconciliation can remove an orphan; never break a committed submission. */ }
            }
            if (exception is MissingCreatorException) return new SnapshotCreationResult.AccountUnavailable();
            throw;
        }
        return new SnapshotCreationResult.Created(snapshot.Id, snapshot.ModerationStatus);
    }

    // Routes missing creators through transaction disposal and upload cleanup before returning AccountUnavailable.
    // Returning directly from the creation block would skip cleanup of an already uploaded source file.
    private sealed class MissingCreatorException : Exception;
}

