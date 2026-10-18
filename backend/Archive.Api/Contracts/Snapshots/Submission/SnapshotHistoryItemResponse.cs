using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Archive.Api.Contracts.Users;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>One ordered snapshot lifecycle event.</summary>
[Description("One ordered snapshot lifecycle event.")]
public sealed class SnapshotHistoryItemResponse
{
    [JsonPropertyName("sequence")]
    [Description("Snapshot-local event sequence.")]
    [Range(1, long.MaxValue)]
    public required long Sequence { get; init; }

    [JsonPropertyName("createdAt")]
    [Description("Event creation time, including the UTC offset.")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("actor")]
    [Description("Public information about the acting account. Null for system events and owner-visible history.")]
    public required PublicUserResponse? Actor { get; init; }

    [JsonPropertyName("content")]
    [Description("Open, versioned event envelope.")]
    public required SnapshotHistoryContentResponse Content { get; init; }
}

/// <summary>Stable envelope around an extensible history payload.</summary>
[Description("Stable envelope around an extensible history payload.")]
public sealed class SnapshotHistoryContentResponse
{
    [JsonPropertyName("type")]
    [Description("Open event type identifier. Clients must tolerate unknown values.")]
    public required string Type { get; init; }

    [JsonPropertyName("version")]
    [Description("Positive schema version scoped to the event type.")]
    [Range(1, int.MaxValue)]
    public required int Version { get; init; }

    [JsonPropertyName("data")]
    [Description("Event-specific JSON object. Clients must tolerate additional fields.")]
    public required JsonElement Data { get; init; }
}
