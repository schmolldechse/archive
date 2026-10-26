using Archive.Api.Contracts.Snapshots;
using Archive.Api.Contracts.Snapshots.Processing;
using Archive.Api.Contracts.Snapshots.Search;
using Archive.Core.Models.Processing;
using Archive.Core.Models.Snapshots;
using Riok.Mapperly.Abstractions;
using Archive.Api.Options;

namespace Archive.Api.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class SnapshotMapper(PublicUserMapper publicUserMapper, SnapshotContentOptions snapshotContent)
{
    [UseMapper]
    private PublicUserMapper PublicUserMapper { get; } = publicUserMapper;

    public partial SnapshotSearch ToSearch(SnapshotSearchRequest request);

    [MapperIgnoreSource(nameof(SnapshotProcessingState.IsTerminal))]
    public partial SnapshotProcessingResponse ToResponse(SnapshotProcessingState state);

    public SnapshotResponse ToResponse(SnapshotDetails snapshot, string publicBaseUrl)
    {
        var resourceUrl = $"{publicBaseUrl.TrimEnd('/')}/api/snapshots/{snapshot.Id}";
        var contentUrl = snapshot.Quality is null
            ? null
            : $"{snapshotContent.PublicOrigin}/api/snapshots/{snapshot.Id}/content/index.html";
        return ToResponse(snapshot,
            snapshot.Quality is null ? null : $"{resourceUrl}/screenshot",
            contentUrl);
    }

    /// <summary>Maps metadata using resource URLs selected by the caller's access scope.</summary>
    public partial SnapshotResponse ToResponse(SnapshotDetails snapshot,
        string? screenshotUrl, string? contentUrl);

    public SnapshotListResponse ToResponse(SnapshotPage page, string publicBaseUrl) =>
        MapPage(page, page.Items.Select(item => ToResponse(item, publicBaseUrl)).ToArray());

    [MapperIgnoreSource(nameof(SnapshotPage.Items))]
    private partial SnapshotListResponse MapPage(SnapshotPage page, IReadOnlyList<SnapshotResponse> items);
}
