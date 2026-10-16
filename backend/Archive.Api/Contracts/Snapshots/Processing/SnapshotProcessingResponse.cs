using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Entities.Processing;

namespace Archive.Api.Contracts.Snapshots.Processing;

[Description("Processing status of a snapshot.")]
public sealed record SnapshotProcessingResponse
{
    [JsonPropertyName("id")]
    [Description("Snapshot identifier.")]
    public required Guid Id { get; init; }

    [JsonPropertyName("status")]
    [Description("Current processing status.")]
    public required ProcessingStatus Status { get; init; }

    [JsonPropertyName("progressStep")]
    [Description("Current processing step.")]
    public required ProgressStep ProgressStep { get; init; }

    [JsonPropertyName("queuePosition")]
    [Description("One-based queue position. Null when the snapshot is not queued.")]
    [Range(1, int.MaxValue)]
    public required int? QueuePosition { get; init; }

    [JsonPropertyName("canCancel")]
    [Description("Whether cancellation can currently be requested.")]
    public required bool CanCancel { get; init; }

    [JsonPropertyName("cancelRequested")]
    [Description("Whether cancellation has been requested.")]
    public required bool CancelRequested { get; init; }

    [JsonPropertyName("startedAt")]
    [Description("Processing start time, including the UTC offset. Null before processing starts.")]
    public required DateTimeOffset? StartedAt { get; init; }
}
