using Archive.Core.Entities;
using Archive.Core.Entities.Authentication;
using Archive.Core.Entities.Processing;
using Archive.Core.Models.Snapshots;
using Archive.Core.Models.Users;
using Archive.Core.Processing;
using Riok.Mapperly.Abstractions;

namespace Archive.Core.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal static partial class SnapshotModelMapper
{
    internal static partial IQueryable<SnapshotDetails> ProjectToDetails(this IQueryable<Snapshot> snapshots);

    [MapProperty(nameof(Snapshot.SnapshotTags), nameof(SnapshotDetails.Tags), Use = nameof(MapTags))]
    [MapProperty(nameof(Snapshot.CreatedBy), nameof(SnapshotDetails.CreatedBy), Use = nameof(MapCreator))]
    [MapProperty(nameof(Snapshot.OriginUrl), nameof(SnapshotDetails.SourceUrl))]
    internal static partial SnapshotDetails MapDetails(Snapshot snapshot);

    internal static partial CaptureInput ToCaptureInput(Snapshot snapshot, string? uploadedObjectKey);

    [MapProperty([nameof(SnapshotSubmissionRow.Snapshot), nameof(Snapshot.Id)],
        nameof(SnapshotSubmissionSummary.Id))]
    [MapProperty([nameof(SnapshotSubmissionRow.Snapshot), nameof(Snapshot.Title)],
        nameof(SnapshotSubmissionSummary.Title))]
    [MapProperty([nameof(SnapshotSubmissionRow.Snapshot), nameof(Snapshot.OriginUrl)],
        nameof(SnapshotSubmissionSummary.SourceUrl))]
    [MapProperty([nameof(SnapshotSubmissionRow.Snapshot), nameof(Snapshot.SourceType)],
        nameof(SnapshotSubmissionSummary.SourceType))]
    [MapProperty([nameof(SnapshotSubmissionRow.Snapshot), nameof(Snapshot.CreatedAt)],
        nameof(SnapshotSubmissionSummary.CreatedAt))]
    [MapProperty([nameof(SnapshotSubmissionRow.Snapshot), nameof(Snapshot.ModerationStatus)],
        nameof(SnapshotSubmissionSummary.ModerationStatus))]
    [MapProperty([nameof(SnapshotSubmissionRow.Snapshot), nameof(Snapshot.Quality)],
        nameof(SnapshotSubmissionSummary.Quality))]
    [MapProperty([nameof(SnapshotSubmissionRow.Snapshot), nameof(Snapshot.CaptureCompletedAt)],
        nameof(SnapshotSubmissionSummary.CaptureCompletedAt))]
    [MapProperty([nameof(SnapshotSubmissionRow.Snapshot), nameof(Snapshot.DurationMilliseconds)],
        nameof(SnapshotSubmissionSummary.DurationMilliseconds))]
    [MapProperty([nameof(SnapshotSubmissionRow.Snapshot), nameof(Snapshot.StorageBytes)],
        nameof(SnapshotSubmissionSummary.StorageBytes))]
    [MapProperty([nameof(SnapshotSubmissionRow.Snapshot), nameof(Snapshot.ResourceCount)],
        nameof(SnapshotSubmissionSummary.ResourceCount))]
    [MapProperty([nameof(SnapshotSubmissionRow.Snapshot), nameof(Snapshot.CreatedBy)],
        nameof(SnapshotSubmissionSummary.CreatedBy),
        Use = nameof(MapCreator))]
    private static partial SnapshotSubmissionSummary ToSubmissionSummary(SnapshotSubmissionRow row);

    internal static partial IQueryable<SnapshotSubmissionSummary> ProjectToSubmissionSummaries(
        this IQueryable<SnapshotSubmissionRow> rows);

    internal static SnapshotSubmissionPage ToSubmissionPage(IReadOnlyList<SnapshotSubmissionSummary> items,
        int total, int page, int pageSize) =>
        MapSubmissionPage(new SnapshotSubmissionPageMapping(items, total, page, pageSize));

    private static partial SnapshotSubmissionPage MapSubmissionPage(SnapshotSubmissionPageMapping source);

    internal static SnapshotModerationDetails ToModerationDetails(SnapshotModerationStatus status,
        DateTimeOffset updatedAt, string? lastDecisionReason) =>
        MapModerationDetails(new SnapshotModerationMapping(status, updatedAt, lastDecisionReason));

    private static partial SnapshotModerationDetails MapModerationDetails(SnapshotModerationMapping source);

    internal static SnapshotSubmissionDetail ToSubmissionDetail(SnapshotDetails snapshot,
        SnapshotModerationDetails moderation, ProcessingStatus processingStatus, string etag) =>
        MapSubmissionDetail(new SnapshotSubmissionDetailMapping(snapshot, moderation, processingStatus, etag));

    private static partial SnapshotSubmissionDetail MapSubmissionDetail(SnapshotSubmissionDetailMapping source);

    private static IReadOnlyList<string> MapTags(List<SnapshotTag> tags) =>
        tags.OrderBy(item => item.Tag.Value).Select(item => item.Tag.Value).ToArray();

    // EF inlines this expression into the projection; only completed profiles are public.
    private static PublicUser? MapCreator(ArchiveUser? creator) =>
        creator == null || creator.RegistrationCompletedAt == null || creator.Handle == null || creator.DisplayName == null
            ? null
            : MapPublicUser(creator);

    [MapPropertyFromSource(nameof(PublicUser.HasProfileImage), Use = nameof(HasProfileImage))]
    private static partial PublicUser MapPublicUser(ArchiveUser user);

    private static bool HasProfileImage(ArchiveUser user) =>
        user.ProfileImage != null && user.ProfileImage.Length > 0;
}

/// <summary>Database projection that combines a snapshot with derived private-list state.</summary>
internal sealed record SnapshotSubmissionRow(Snapshot Snapshot, DateTimeOffset ModerationUpdatedAt,
    ProcessingStatus ProcessingStatus);

/// <summary>Bundles immutable submission-page constructor values for Mapperly.</summary>
internal sealed record SnapshotSubmissionPageMapping(IReadOnlyList<SnapshotSubmissionSummary> Items, int Total,
    int Page, int PageSize);

/// <summary>Bundles immutable moderation constructor values for Mapperly.</summary>
internal sealed record SnapshotModerationMapping(SnapshotModerationStatus Status, DateTimeOffset UpdatedAt,
    string? LastDecisionReason);

/// <summary>Bundles immutable submission-detail constructor values for Mapperly.</summary>
internal sealed record SnapshotSubmissionDetailMapping(SnapshotDetails Snapshot, SnapshotModerationDetails Moderation,
    ProcessingStatus ProcessingStatus, string ETag);
