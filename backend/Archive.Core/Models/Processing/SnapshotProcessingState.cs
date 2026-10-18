using Archive.Core.Entities.Processing;
using Archive.Core.Entities;

namespace Archive.Core.Models.Processing;

public sealed record SnapshotProcessingState(Guid Id, ProcessingStatus Status, ProgressStep ProgressStep,
    int? QueuePosition, bool CanCancel, bool CancelRequested, DateTimeOffset? StartedAt,
    SnapshotModerationStatus ModerationStatus, bool IsPublic)
{
    public bool IsTerminal => Status is not (ProcessingStatus.Queued or ProcessingStatus.Running);
}
