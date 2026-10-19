using Archive.Core.Entities;
using Archive.Core.Entities.Authentication;
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

    private static IReadOnlyList<string> MapTags(List<SnapshotTag> tags) =>
        tags.OrderBy(item => item.Tag.Value).Select(item => item.Tag.Value).ToArray();

    // EF inlines this expression into the projection; only completed profiles are public.
    private static PublicUser? MapCreator(ArchiveUser? creator) =>
        creator == null || creator.RegistrationCompletedAt == null || creator.Handle == null || creator.DisplayName == null
            ? null
            : new PublicUser(creator.Handle, creator.DisplayName,
                creator.ProfileImage != null && creator.ProfileImage.Length > 0, creator.Role);
}
