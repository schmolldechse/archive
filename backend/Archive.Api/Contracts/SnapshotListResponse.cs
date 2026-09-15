using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts;

public sealed record SnapshotListResponse(
    [property: JsonPropertyName("items"), Description("Snapshots in the requested page."), Required] IReadOnlyList<SnapshotResponse> Items,
    [property: JsonPropertyName("total"), Description("Total number of matching snapshots."), Range(0, int.MaxValue)] int Total,
    [property: JsonPropertyName("page"), Description("One-based result page number."), Range(1, int.MaxValue)] int Page,
    [property: JsonPropertyName("pageSize"), Description("Number of snapshots per result page."), Range(1, 100)] int PageSize,
    [property: JsonPropertyName("indexTotal"), Description("Total number of published snapshots, independent of search filters."), Range(0, int.MaxValue)] int IndexTotal,
    [property: JsonPropertyName("recentCaptureTimes"), Description("Up to four newest capture timestamps in the published register, independent of search and pagination."), Required] IReadOnlyList<DateTimeOffset> RecentCaptureTimes,
    [property: JsonPropertyName("captureDistribution"), Description("Capture distribution under text and metadata filters, ignoring capture-time filters and pagination."), Required] CaptureDistributionResponse CaptureDistribution);
