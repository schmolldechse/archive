using Archive.Api.Contracts;
using Archive.Core.Entities;
using Archive.Core.Models.Snapshots;
using Riok.Mapperly.Abstractions;

namespace Archive.Api.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class SnapshotMapper(ArchiveCreatorMapper creatorMapper)
{
    [UseMapper]
    private ArchiveCreatorMapper CreatorMapper { get; } = creatorMapper;

    public partial SnapshotSearch ToSearch(SnapshotSearchRequest request);

    [MapEnum(EnumMappingStrategy.ByName)]
    private partial SnapshotSortOrder ToSortOrder(SnapshotOrder order);

    public SnapshotResponse ToResponse(SnapshotDetails snapshot, string publicBaseUrl)
    {
        var resourceUrl = $"{publicBaseUrl.TrimEnd('/')}/api/snapshots/{snapshot.Id}";
        return MapSnapshot(snapshot,
            snapshot.SourceType == SourceType.Url ? snapshot.OriginUrl : null,
            snapshot.SourceType != SourceType.Url ? snapshot.OriginUrl : null,
            snapshot.Quality is null ? null : $"{resourceUrl}/screenshot",
            snapshot.Quality is null ? null : $"{resourceUrl}/content/index.html");
    }

    public SnapshotListResponse ToResponse(SnapshotPage page, string publicBaseUrl) =>
        MapPage(page, page.Items.Select(item => ToResponse(item, publicBaseUrl)).ToArray());

    [MapperIgnoreSource(nameof(SnapshotDetails.OriginUrl))]
    private partial SnapshotResponse MapSnapshot(SnapshotDetails snapshot, string? sourceUrl,
        string? originalLink, string? screenshotUrl, string? contentUrl);

    [MapperIgnoreSource(nameof(SnapshotPage.Items))]
    private partial SnapshotListResponse MapPage(SnapshotPage page, IReadOnlyList<SnapshotResponse> items);
}
