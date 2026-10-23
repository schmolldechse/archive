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
    [Description("Public information about the acting registered account. Null for system events or unavailable profiles.")]
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

/// <summary>API-safe attribution payload resolved from internal identifiers at read time.</summary>
[Description("Resolved attribution change. Internal user identifiers are never returned.")]
public sealed class SnapshotAttributionUpdatedResponseData
{
    [JsonPropertyName("previousUser")]
    [Description("Previous current public profile, or null when anonymous, deleted or unavailable.")]
    public required PublicUserResponse? PreviousUser { get; init; }

    [JsonPropertyName("user")]
    [Description("New current public profile, or null when account attribution was removed or is unavailable.")]
    public required PublicUserResponse? User { get; init; }

    [JsonPropertyName("removed")]
    [Description("Whether this event explicitly removed account attribution.")]
    public required bool Removed { get; init; }

    [JsonPropertyName("redacted")]
    [Description("Whether a superseded public attribution target was intentionally withheld.")]
    public required bool Redacted { get; init; }
}
