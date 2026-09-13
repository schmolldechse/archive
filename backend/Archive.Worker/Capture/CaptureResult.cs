using Archive.Core.Entities;

namespace Archive.Worker.Capture;

public sealed record CaptureResult(
    SnapshotQuality Quality,
    string ContentPrefix,
    long StorageBytes,
    int ResourceCount,
    long DurationMilliseconds,
    IReadOnlyList<string> StoredObjectKeys);

public sealed record CapturedResource(string Url, string ContentType, byte[] Body);
