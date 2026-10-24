using Archive.Core.Entities;
using Archive.Core.Processing;

namespace Archive.Core.Models.Processing;

public sealed record ClaimedSnapshot(CaptureInput Input, Guid OwnerToken, DateTimeOffset StartedAt);
public sealed record SnapshotCaptureResult(SnapshotQuality Quality, string ContentPrefix,
    long DurationMilliseconds, long StorageBytes, int ResourceCount, int? ReplayVersion);
public enum CancellationOutcome { Accepted, NotCancellable, Missing }
public sealed record ProcessingCancellation(CancellationOutcome Outcome, SnapshotProcessingState? State);
public enum ProcessingCompletion { Stored, NotStored, OwnershipLost }
