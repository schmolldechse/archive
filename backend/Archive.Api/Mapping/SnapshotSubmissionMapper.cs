using Archive.Api.Contracts.Snapshots;
using Archive.Api.Contracts.Snapshots.Submission;
using Archive.Core.Models.Snapshots;
using Riok.Mapperly.Abstractions;

namespace Archive.Api.Mapping;

/// <summary>Maps private snapshot submission projections to public API contracts.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class SnapshotSubmissionMapper(SnapshotMapper snapshotMapper, PublicUserMapper publicUserMapper)
{
    [UseMapper]
    private PublicUserMapper PublicUserMapper { get; } = publicUserMapper;

    /// <summary>Maps a paged private submission result.</summary>
    public partial SnapshotSubmissionListResponse ToResponse(SnapshotSubmissionPage page);

    /// <summary>Maps a paged history result without exposing internal entry identifiers.</summary>
    public partial SnapshotHistoryResponse ToResponse(SnapshotHistoryPage page);

    /// <summary>Maps private details with resource URLs scoped to the calling controller.</summary>
    public SnapshotSubmissionDetailResponse ToResponse(SnapshotSubmissionDetail detail, string resourceBaseUrl)
    {
        var normalizedBaseUrl = resourceBaseUrl.TrimEnd('/');
        var snapshot = snapshotMapper.ToResponse(detail.Snapshot,
            detail.Snapshot.Quality is null ? null : $"{normalizedBaseUrl}/screenshot",
            detail.Snapshot.Quality is null ? null : $"{normalizedBaseUrl}/content/index.html");
        return MapDetail(detail, snapshot);
    }

    [MapperIgnoreSource(nameof(SnapshotSubmissionDetail.Snapshot))]
    [MapperIgnoreSource(nameof(SnapshotSubmissionDetail.ETag))]
    private partial SnapshotSubmissionDetailResponse MapDetail(SnapshotSubmissionDetail detail,
        SnapshotResponse snapshot);
}
