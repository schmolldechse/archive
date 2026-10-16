using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Snapshots.Search;

[Description("Annual capture counts under text and metadata filters.")]
public sealed record CaptureDistributionResponse
{
    [JsonPropertyName("total")]
    [Description("Number of matches before capture-time filters and pagination.")]
    [Range(0, int.MaxValue)]
    public required int Total { get; init; }

    [JsonPropertyName("firstYear")]
    [Description("Earliest populated year. Null when there are no matches.")]
    public required int? FirstYear { get; init; }

    [JsonPropertyName("lastYear")]
    [Description("Latest populated year. Null when there are no matches.")]
    public required int? LastYear { get; init; }

    [JsonPropertyName("years")]
    [Description("Annual counts before capture-time filters and pagination.")]
    public required IReadOnlyList<CaptureYearResponse> Years { get; init; }
}
