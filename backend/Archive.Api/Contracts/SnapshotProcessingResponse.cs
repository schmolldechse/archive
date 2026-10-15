using System.ComponentModel.DataAnnotations;
using Archive.Core.Entities.Processing;
using Archive.Core.Models.Processing;

namespace Archive.Api.Contracts;

public sealed record SnapshotProcessingResponse(
    [property: Required] Guid Id,
    [property: Required] ProcessingStatus Status,
    [property: Required] ProgressStep ProgressStep,
    int? QueuePosition, bool CanCancel, bool CancelRequested,
    DateTimeOffset? StartedAt)
{
    public static SnapshotProcessingResponse From(SnapshotProcessingState state) => new(state.Id, state.Status,
        state.ProgressStep, state.QueuePosition, state.CanCancel, state.CancelRequested, state.StartedAt);
}
