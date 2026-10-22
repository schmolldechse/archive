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
    public SnapshotHistoryResponse ToResponse(SnapshotHistoryPage page) => new()
    {
        Items = page.Items.Select(ToResponse).ToArray(),
        Total = page.Total,
        Page = page.Page,
        PageSize = page.PageSize
    };

    /// <summary>Maps registered owner candidates without exposing internal user identifiers.</summary>
    public SnapshotOwnerListResponse ToOwnerResponse(UserListPage page) => new()
    {
        Items = page.Items.Select(user => PublicUserMapper.ToResponse(user)).ToArray(),
        Total = page.Total,
        Page = page.Page,
        PageSize = page.PageSize
    };

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

    private SnapshotHistoryItemResponse ToResponse(SnapshotHistoryItem item)
    {
        var data = item.Content.Data;
        if (item.Content.Type == SnapshotHistoryTypes.AttributionUpdated)
        {
            // Attribution payloads persist internal identifiers. Never pass their raw JSON through the public contract,
            // including when a newer event version reaches an older API deployment.
            data = item.Content.Version == 1
                ? JsonSerializer.SerializeToElement(new SnapshotAttributionUpdatedResponseData
                {
                    PreviousUser = item.Attribution?.PreviousUser is { } previous
                        ? PublicUserMapper.ToResponse(previous)
                        : null,
                    User = item.Attribution?.User is { } user ? PublicUserMapper.ToResponse(user) : null,
                    Removed = item.Attribution?.Removed ?? false,
                    Redacted = item.Attribution?.Redacted ?? false
                }, HistoryJson)
                : JsonSerializer.SerializeToElement(new { }, HistoryJson);
        }

        return new SnapshotHistoryItemResponse
        {
            Sequence = item.Sequence,
            CreatedAt = item.CreatedAt,
            Actor = item.Actor is null ? null : PublicUserMapper.ToResponse(item.Actor),
            Content = new SnapshotHistoryContentResponse
            {
                Type = item.Content.Type,
                Version = item.Content.Version,
                Data = data
            }
        };
    }

    private static JsonSerializerOptions CreateHistoryJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ArchiveJson.Configure(options);
        return options;
    }
}
