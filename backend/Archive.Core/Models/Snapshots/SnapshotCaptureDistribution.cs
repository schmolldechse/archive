namespace Archive.Core.Models.Snapshots;

public sealed record SnapshotCaptureYear(int Year, int Count);

public sealed record SnapshotCaptureDistribution(int Total, int? FirstYear, int? LastYear, IReadOnlyList<SnapshotCaptureYear> Years);
