using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts;

public sealed record SnapshotListResponse(
    [property: JsonPropertyName("items"), Description("Snapshots in the requested page."), Required] IReadOnlyList<SnapshotResponse> Items,
    [property: JsonPropertyName("total"), Description("Total number of matching snapshots."), Range(0, int.MaxValue)] int Total,
    [property: JsonPropertyName("page"), Description("One-based result page number."), Range(1, int.MaxValue)] int Page,
    [property: JsonPropertyName("pageSize"), Description("Number of snapshots per result page."), Range(1, 100)] int PageSize);
