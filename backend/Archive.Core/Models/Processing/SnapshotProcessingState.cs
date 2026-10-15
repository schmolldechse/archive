using Archive.Core.Entities.Processing;

namespace Archive.Core.Models.Processing;

public sealed record SnapshotProcessingState(Guid Id, ProcessingStatus Status, ProgressStep ProgressStep,
    int? QueuePosition, bool CanCancel, bool CancelRequested, DateTimeOffset? StartedAt)
{
    public bool IsTerminal => Status is not (ProcessingStatus.Queued or ProcessingStatus.Running);
}
