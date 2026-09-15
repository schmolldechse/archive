using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts;

public sealed record CaptureYearResponse(
    [property: JsonPropertyName("year"), Description("Calendar year of persisted capture timestamps.")] int Year,
    [property: JsonPropertyName("count"), Description("Number of matching captures in this year."), Range(0, int.MaxValue)] int Count);

public sealed record CaptureDistributionResponse(
    [property: JsonPropertyName("total"), Description("Matches under text and metadata filters, ignoring capture-time filters."), Range(0, int.MaxValue)] int Total,
    [property: JsonPropertyName("firstYear"), Description("Earliest populated year; null when empty.")] int? FirstYear,
    [property: JsonPropertyName("lastYear"), Description("Latest populated year; null when empty.")] int? LastYear,
    [property: JsonPropertyName("years"), Description("Annual capture counts before capture-time filters and pagination."), Required] IReadOnlyList<CaptureYearResponse> Years);
