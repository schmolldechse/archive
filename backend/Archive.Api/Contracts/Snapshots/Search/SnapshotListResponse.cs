using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Models.Snapshots;

namespace Archive.Api.Contracts.Snapshots.Search;

[Description("A page of snapshots and capture counts.")]
public sealed record SnapshotListResponse
{
    [JsonPropertyName("items")]
    [Description("Snapshots in the requested page.")]
    public required IReadOnlyList<SnapshotResponse> Items { get; init; }

    [JsonPropertyName("total")]
    [Description("Number of snapshots matching all search filters.")]
    [Range(0, int.MaxValue)]
    public required int Total { get; init; }

    [JsonPropertyName("page")]
    [Description("One-based result page.")]
    [Range(1, SnapshotValidation.MaxPage)]
    public required int Page { get; init; }

    [JsonPropertyName("pageSize")]
    [Description("Number of snapshots per page.")]
    [Range(1, SnapshotValidation.MaxPageSize)]
    public required int PageSize { get; init; }

    [JsonPropertyName("indexTotal")]
    [Description("Total number of published snapshots, independent of search filters.")]
    [Range(0, int.MaxValue)]
    public required int IndexTotal { get; init; }

    [JsonPropertyName("recentCaptureTimes")]
    [Description("Up to four newest published snapshot creation times, independent of search filters.")]
    public required IReadOnlyList<DateTimeOffset> RecentCaptureTimes { get; init; }

    [JsonPropertyName("captureDistribution")]
    [Description("Annual capture counts before capture-time filters and pagination.")]
    public required CaptureDistributionResponse CaptureDistribution { get; init; }
}
