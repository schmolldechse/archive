using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Snapshots.Processing;

[Description("Payload of a named SSE error event.")]
public sealed record SnapshotProcessingErrorResponse
{
    [JsonPropertyName("status")]
    [Description("HTTP status code describing the stream error.")]
    [Range(400, 599)]
    public required int Status { get; init; }

    [JsonPropertyName("title")]
    [Description("Short description of the stream error.")]
    public required string Title { get; init; }
}
