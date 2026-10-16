using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Health;

[Description("Service health status.")]
public sealed record HealthResponse
{
    [JsonPropertyName("status")]
    [Description("Service status. A successful response contains ok.")]
    public required string Status { get; init; }
}
