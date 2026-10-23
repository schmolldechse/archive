using System.Text.Json;
using Archive.Core.Entities;
using Archive.Core.Entities.Authentication;
using Archive.Core.Models.Snapshots;
using Archive.Core.Models.Snapshots.History;
using Archive.Core.Models.Users;
using Riok.Mapperly.Abstractions;

namespace Archive.Core.Mapping;

/// <summary>Maps persistence fields without interpreting or converting history content.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal static partial class SnapshotHistoryMapper
{
    /// <summary>Leaves identity generation to the entity and sequence and timestamp assignment to the repository.</summary>
    [MapperIgnoreTarget(nameof(SnapshotHistoryEntry.Id))]
    [MapperIgnoreTarget(nameof(SnapshotHistoryEntry.Sequence))]
    [MapperIgnoreTarget(nameof(SnapshotHistoryEntry.CreatedAt))]
    [MapperIgnoreTarget(nameof(SnapshotHistoryEntry.Actor))]
    [MapProperty(nameof(Snapshot.Id), nameof(SnapshotHistoryEntry.SnapshotId))]
    [MapPropertyFromSource(nameof(SnapshotHistoryEntry.Snapshot))]
    internal static partial SnapshotHistoryEntry ToEntity(Snapshot snapshot, JsonElement content, Guid? actorUserId);

    [MapProperty(nameof(SnapshotHistoryEntry.Actor), nameof(SnapshotHistoryRow.Actor), Use = nameof(MapActor))]
    private static partial SnapshotHistoryRow ToRow(SnapshotHistoryEntry entry);

    internal static partial IQueryable<SnapshotHistoryRow> ProjectToRows(
        this IQueryable<SnapshotHistoryEntry> entries);

    private static partial PublicSnapshotHistoryRow ToPublicRow(SnapshotHistoryEntry entry);

    internal static partial IQueryable<PublicSnapshotHistoryRow> ProjectToPublicRows(
        this IQueryable<SnapshotHistoryEntry> entries);

    [MapValue(nameof(SnapshotHistoryRow.Actor), null)]
    internal static partial SnapshotHistoryRow ToRow(PublicSnapshotHistoryRow row);

    [MapperIgnoreSource(nameof(SnapshotHistoryRow.Content))]
    internal static partial SnapshotHistoryItem ToModel(SnapshotHistoryRow row, SnapshotHistoryContent content,
        SnapshotHistoryAttribution? attribution);

    internal static SnapshotHistoryPage ToPage(IReadOnlyList<SnapshotHistoryItem> items, int total, int page,
        int pageSize) => MapPage(new SnapshotHistoryPageMapping(items, total, page, pageSize));

    private static partial SnapshotHistoryPage MapPage(SnapshotHistoryPageMapping source);

    internal static SnapshotHistoryAttribution ToAttribution(PublicUser? previousUser, PublicUser? user,
        bool removed, bool redacted) =>
        MapAttribution(new SnapshotHistoryAttributionMapping(previousUser, user, removed, redacted));

    private static partial SnapshotHistoryAttribution MapAttribution(SnapshotHistoryAttributionMapping source);

    internal static partial PublicDetailsUpdatedData ToPublicDetails(SnapshotDetailsUpdatedData data);

    internal static partial PublicModeratedData ToPublicModerated(SnapshotModerationStatus status);

    internal static PublicResubmittedData PublicResubmitted { get; } = new();

    internal static PublicSourceUpdatedData ToPublicSource(string? sourceUrl, bool reviewRequired) =>
        MapPublicSource(new PublicSourceUpdatedMapping(sourceUrl, reviewRequired));

    private static partial PublicSourceUpdatedData MapPublicSource(PublicSourceUpdatedMapping source);

    private static PublicUser? MapActor(ArchiveUser? actor) =>
        actor == null || actor.RegistrationCompletedAt == null || actor.Handle == null || actor.DisplayName == null
            ? null
            : MapPublicUser(actor);

    // Keep this mapping local so Mapperly can inline it into the EF projection.
    [MapPropertyFromSource(nameof(PublicUser.HasProfileImage), Use = nameof(HasProfileImage))]
    private static partial PublicUser MapPublicUser(ArchiveUser user);

    private static bool HasProfileImage(ArchiveUser user) =>
        user.ProfileImage != null && user.ProfileImage.Length > 0;
}

/// <summary>Database projection used before versioned history payloads are interpreted.</summary>
internal sealed record SnapshotHistoryRow(long Sequence, DateTimeOffset CreatedAt, JsonElement Content,
    PublicUser? Actor);

/// <summary>Public database projection that excludes reviewer identity before materialization.</summary>
internal sealed record PublicSnapshotHistoryRow(long Sequence, DateTimeOffset CreatedAt, JsonElement Content);

/// <summary>Bundles immutable history-page constructor values for Mapperly.</summary>
internal sealed record SnapshotHistoryPageMapping(IReadOnlyList<SnapshotHistoryItem> Items, int Total, int Page,
    int PageSize);

/// <summary>Bundles resolved attribution values for Mapperly.</summary>
internal sealed record SnapshotHistoryAttributionMapping(PublicUser? PreviousUser, PublicUser? User, bool Removed,
    bool Redacted);

/// <summary>Bundles the API-safe source-update values for Mapperly.</summary>
internal sealed record PublicSourceUpdatedMapping(string? SourceUrl, bool ReviewRequired);

/// <summary>Redacted source-update payload used only for the public history projection.</summary>
internal sealed record PublicSourceUpdatedData(string? SourceUrl, bool ReviewRequired) : SnapshotHistoryData
{
    internal override string Type => SnapshotHistoryTypes.SourceUpdated;
}

/// <summary>Public details-update payload without complete before-and-after metadata values.</summary>
internal sealed record PublicDetailsUpdatedData(
    IReadOnlyList<SnapshotDetailField> Fields,
    IReadOnlyList<string> AddedTags,
    IReadOnlyList<string> RemovedTags) : SnapshotHistoryData
{
    internal override string Type => SnapshotHistoryTypes.DetailsUpdated;
    internal override int Version => 2;
}

/// <summary>Public approval payload without an earlier moderation decision or review note.</summary>
internal sealed record PublicModeratedData(SnapshotModerationStatus Status) : SnapshotHistoryData
{
    internal override string Type => SnapshotHistoryTypes.Moderated;
}

/// <summary>Public resubmission marker without private moderation-state transitions.</summary>
internal sealed record PublicResubmittedData() : SnapshotHistoryData
{
    internal override string Type => SnapshotHistoryTypes.Resubmitted;
}
