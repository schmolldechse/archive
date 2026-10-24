using Archive.Core.Entities;

namespace Archive.Worker.Capture;

public sealed record CaptureResult(
    SnapshotQuality Quality,
    string ContentPrefix,
    long StorageBytes,
    int ResourceCount,
    long DurationMilliseconds,
    int? ReplayVersion,
    IReadOnlyList<string> StoredObjectKeys);
