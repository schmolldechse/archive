using System.Text.Json;
using Archive.Api.Contracts.Snapshots;
using Archive.Api.Contracts.Snapshots.Submission;
using Archive.Api.Serialization;
using Archive.Core.Models.Snapshots;
using Archive.Core.Models.Snapshots.History;
using Archive.Core.Models.Users;
using Riok.Mapperly.Abstractions;

namespace Archive.Api.Mapping;

/// <summary>Maps private snapshot submission projections to public API contracts.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class SnapshotSubmissionMapper(SnapshotMapper snapshotMapper, PublicUserMapper publicUserMapper)
{
    private static readonly JsonSerializerOptions HistoryJson = CreateHistoryJson();

    [UseMapper]
    private PublicUserMapper PublicUserMapper { get; } = publicUserMapper;

    /// <summary>Maps a paged private submission result.</summary>
    public partial SnapshotSubmissionListResponse ToResponse(SnapshotSubmissionPage page);

    /// <summary>Maps a paged history result and replaces durable user IDs with public user contracts.</summary>
    public partial SnapshotHistoryResponse ToResponse(SnapshotHistoryPage page);

    /// <summary>Maps registered owner candidates without exposing internal user identifiers.</summary>
    public partial SnapshotOwnerListResponse ToOwnerResponse(UserListPage page);

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

    [MapperIgnoreSource(nameof(SnapshotHistoryItem.Attribution))]
    [MapPropertyFromSource(nameof(SnapshotHistoryItemResponse.Content), Use = nameof(BuildHistoryContent))]
    private partial SnapshotHistoryItemResponse MapHistoryItem(SnapshotHistoryItem item);

    /// <summary>Sanitizes event-specific data before Mapperly creates the public content envelope.</summary>
    private SnapshotHistoryContentResponse BuildHistoryContent(SnapshotHistoryItem item)
    {
        var data = item.Content.Data;
        if (item.Content.Type == SnapshotHistoryTypes.AttributionUpdated)
        {
            // Attribution payloads persist internal identifiers. Never pass their raw JSON through the public contract,
            // including when a newer event version reaches an older API deployment.
            data = item.Content.Version == 1
                ? JsonSerializer.SerializeToElement(MapAttribution(item.Attribution ?? EmptyAttribution), HistoryJson)
                : JsonSerializer.SerializeToElement(new { }, HistoryJson);
        }

        return MapContentEnvelope(item.Content, data);
    }

    private static readonly SnapshotHistoryAttribution EmptyAttribution = new(null, null, false, false);

    [MapperIgnoreSource(nameof(SnapshotHistoryContent.Data))]
    private partial SnapshotHistoryContentResponse MapContentEnvelope(SnapshotHistoryContent content, JsonElement data);

    private partial SnapshotAttributionUpdatedResponseData MapAttribution(SnapshotHistoryAttribution attribution);

    private static JsonSerializerOptions CreateHistoryJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ArchiveJson.Configure(options);
        return options;
    }
}
