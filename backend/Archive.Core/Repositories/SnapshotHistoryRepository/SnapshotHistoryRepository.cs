using Archive.Core.Entities;
using Archive.Core.Models.Snapshots;
using Archive.Core.Mapping;
using Archive.Core.Models.Snapshots.History;
using Archive.Core.Models.Users;
using Microsoft.EntityFrameworkCore;
using Archive.Core.Serialization;

namespace Archive.Core.Repositories.SnapshotHistoryRepository;

/// <summary>Serializes history appends per snapshot using the shared scoped database context.</summary>
internal sealed class SnapshotHistoryRepository(DataContext database) : ISnapshotHistoryRepository
{
    public async Task<SnapshotHistoryEntry> AppendAsync(Snapshot snapshot, SnapshotHistoryData data, Guid? actorUserId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (database.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Snapshot history must be appended within a transaction.");

        var snapshotState = database.Entry(snapshot).State;
        if (snapshotState == EntityState.Detached)
            throw new InvalidOperationException("Track the snapshot before appending its history.");

        var history = SnapshotHistoryMapper.ToEntity(snapshot, SnapshotHistorySerializer.Serialize(data), actorUserId);

        long persistedSequence = 0;
        if (snapshotState != EntityState.Added)
        {
            // Lock the parent even when it has no history; concurrent appends wait for this transaction.
            await database.Database
                .SqlQuery<Guid>($"""SELECT id AS "Value" FROM archive.snapshots WHERE id = {snapshot.Id} FOR UPDATE""")
                .SingleAsync(cancellationToken);
            persistedSequence = await database.SnapshotHistory
                .Where(entry => entry.SnapshotId == snapshot.Id)
                .MaxAsync(entry => (long?)entry.Sequence, cancellationToken) ?? 0;
        }

        // Pending entries also reserve positions, allowing several events to share one SaveChanges call.
        var pendingSequence = database.SnapshotHistory.Local
            .Where(entry => entry.SnapshotId == snapshot.Id)
            .Max(entry => (long?)entry.Sequence) ?? 0;
        history.Sequence = checked(Math.Max(persistedSequence, pendingSequence) + 1);
        history.CreatedAt = DateTimeOffset.UtcNow;

        database.SnapshotHistory.Add(history);
        return history;
    }

    public Task<SnapshotHistoryPage?> ReadOwnAsync(Guid userId, Guid snapshotId, int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        ReadAsync(snapshotId, userId, publicOnly: false, includeUnknown: false, page, pageSize, cancellationToken);

    public Task<SnapshotHistoryPage?> ReadAdminAsync(Guid snapshotId, int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        ReadAsync(snapshotId, null, publicOnly: false, includeUnknown: true, page, pageSize, cancellationToken);

    public Task<SnapshotHistoryPage?> ReadPublicAsync(Guid snapshotId, int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        ReadAsync(snapshotId, null, publicOnly: true, includeUnknown: false, page, pageSize, cancellationToken);

    /// <summary>Applies the audience visibility boundary before pagination and resolves stored user IDs afterwards.</summary>
    private async Task<SnapshotHistoryPage?> ReadAsync(Guid snapshotId, Guid? ownerId, bool publicOnly,
        bool includeUnknown, int page, int pageSize, CancellationToken token)
    {
        var snapshots = database.Snapshots.AsNoTracking().Where(snapshot => snapshot.Id == snapshotId);
        if (publicOnly)
            snapshots = snapshots.Where(snapshot => snapshot.ModerationStatus == SnapshotModerationStatus.Approved &&
                snapshot.Quality != null);
        else if (ownerId is not null)
            snapshots = snapshots.Where(snapshot => snapshot.CreatedByUserId == ownerId);
        var snapshotState = await snapshots.Select(snapshot => new
        {
            snapshot.OriginUrl,
            snapshot.CreatedByUserId
        }).SingleOrDefaultAsync(token);
        if (snapshotState is null) return null;

        var query = database.SnapshotHistory.AsNoTracking().Where(entry => entry.SnapshotId == snapshotId);
        if (!includeUnknown)
        {
            query = query.Where(entry =>
                entry.Content.GetProperty("type").GetString() == SnapshotHistoryTypes.Submitted &&
                    entry.Content.GetProperty("version").GetInt32() == 1 ||
                entry.Content.GetProperty("type").GetString() == SnapshotHistoryTypes.Stored &&
                    entry.Content.GetProperty("version").GetInt32() == 1 ||
                entry.Content.GetProperty("type").GetString() == SnapshotHistoryTypes.Resubmitted &&
                    entry.Content.GetProperty("version").GetInt32() == 1 ||
                entry.Content.GetProperty("type").GetString() == SnapshotHistoryTypes.DetailsUpdated &&
                    (entry.Content.GetProperty("version").GetInt32() == 1 ||
                     entry.Content.GetProperty("version").GetInt32() == 2) ||
                entry.Content.GetProperty("type").GetString() == SnapshotHistoryTypes.SourceUpdated &&
                    entry.Content.GetProperty("version").GetInt32() == 1 ||
                entry.Content.GetProperty("type").GetString() == SnapshotHistoryTypes.AttributionUpdated &&
                    entry.Content.GetProperty("version").GetInt32() == 1 ||
                entry.Content.GetProperty("type").GetString() == SnapshotHistoryTypes.ProcessingFinished &&
                    (entry.Content.GetProperty("version").GetInt32() == 1 ||
                     entry.Content.GetProperty("version").GetInt32() == 2) ||
                entry.Content.GetProperty("type").GetString() == SnapshotHistoryTypes.Moderated &&
                    entry.Content.GetProperty("version").GetInt32() == 1 &&
                    (!publicOnly || entry.Content.GetProperty("data").GetProperty("status").GetString() == "APPROVED"));
        }

        var total = await query.CountAsync(token);
        var pageQuery = query.OrderByDescending(entry => entry.Sequence)
            .Skip((page - 1) * pageSize).Take(pageSize);
        SnapshotHistoryRow[] rows;
        if (publicOnly)
        {
            var publicRows = await pageQuery.ProjectToPublicRows().ToArrayAsync(token);
            rows = publicRows.Select(SnapshotHistoryMapper.ToRow).ToArray();
        }
        else
        {
            rows = await pageQuery.ProjectToRows().ToArrayAsync(token);
        }

        var attributionData = rows
            .Where(row =>
            {
                var content = SnapshotHistorySerializer.Deserialize(row.Content);
                return content.Type == SnapshotHistoryTypes.AttributionUpdated && content.Version == 1;
            })
            .Select(row => SnapshotHistorySerializer.Deserialize<SnapshotAttributionUpdatedData>(row.Content))
            .ToArray();
        var userIds = attributionData.SelectMany(data => new Guid?[] { data.PreviousUserId, data.UserId })
            .OfType<Guid>().Distinct().ToArray();
        var users = userIds.Length == 0
            ? new Dictionary<Guid, PublicUser>()
            : await database.Users.AsNoTracking()
                .Where(user => userIds.Contains(user.Id) && user.RegistrationCompletedAt != null &&
                    user.Handle != null && user.DisplayName != null)
                .ProjectToProfileEntries()
                .ToDictionaryAsync(row => row.Id, row => row.Profile, token);

        var items = rows.Select(row =>
        {
            var content = SnapshotHistorySerializer.Deserialize(row.Content);
            SnapshotHistoryAttribution? attribution = null;
            if (content.Type == SnapshotHistoryTypes.AttributionUpdated && content.Version == 1)
            {
                var data = SnapshotHistorySerializer.Deserialize<SnapshotAttributionUpdatedData>(row.Content);
                users.TryGetValue(data.PreviousUserId ?? Guid.Empty, out var previousUser);
                users.TryGetValue(data.UserId ?? Guid.Empty, out var user);
                var redacted = publicOnly && data.UserId != snapshotState.CreatedByUserId;
                if (redacted)
                    user = null;
                attribution = SnapshotHistoryMapper.ToAttribution(publicOnly ? null : previousUser, user,
                    data.UserId is null, redacted);
            }
            else if (publicOnly && content.Type == SnapshotHistoryTypes.DetailsUpdated && content.Version == 2)
            {
                var data = SnapshotHistorySerializer.Deserialize<SnapshotDetailsUpdatedData>(row.Content);
                // Old metadata values remain available to the owner and administrators only.
                content = SnapshotHistorySerializer.Deserialize(SnapshotHistorySerializer.Serialize(
                    SnapshotHistoryMapper.ToPublicDetails(data)));
            }
            else if (publicOnly && content.Type == SnapshotHistoryTypes.Moderated && content.Version == 1)
            {
                var data = SnapshotHistorySerializer.Deserialize<SnapshotModeratedData>(row.Content);
                content = SnapshotHistorySerializer.Deserialize(
                    SnapshotHistorySerializer.Serialize(SnapshotHistoryMapper.ToPublicModerated(data.Status)));
            }
            else if (publicOnly && content.Type == SnapshotHistoryTypes.SourceUpdated && content.Version == 1)
            {
                var data = SnapshotHistorySerializer.Deserialize<SnapshotSourceUpdatedData>(row.Content);
                // Only the currently published source may be exposed. Rejected or superseded
                // intermediate URLs remain part of the private audit record.
                var publishedSource = string.Equals(data.SourceUrl, snapshotState.OriginUrl,
                    StringComparison.Ordinal) ? data.SourceUrl : null;
                content = SnapshotHistorySerializer.Deserialize(SnapshotHistorySerializer.Serialize(
                    SnapshotHistoryMapper.ToPublicSource(publishedSource, data.ReviewRequired)));
            }
            else if (publicOnly && content.Type == SnapshotHistoryTypes.Resubmitted && content.Version == 1)
            {
                // A public record may show that another review was requested without
                // revealing a preceding rejection state.
                content = SnapshotHistorySerializer.Deserialize(
                    SnapshotHistorySerializer.Serialize(SnapshotHistoryMapper.PublicResubmitted));
            }

            return SnapshotHistoryMapper.ToModel(row, content, attribution);
        }).ToArray();
        return SnapshotHistoryMapper.ToPage(items, total, page, pageSize);
    }
}
