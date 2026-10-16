using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Snapshots.Search;

[Description("Capture count for one calendar year.")]
public sealed record CaptureYearResponse
{
    [JsonPropertyName("year")]
    [Description("Calendar year of the stored snapshot creation times.")]
    public required int Year { get; init; }

    [JsonPropertyName("count")]
    [Description("Number of matching captures in the year.")]
    [Range(0, int.MaxValue)]
    public required int Count { get; init; }
}
