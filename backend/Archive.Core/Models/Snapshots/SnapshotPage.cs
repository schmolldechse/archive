namespace Archive.Core.Models.Snapshots;

public sealed record SnapshotPage(IReadOnlyList<SnapshotDetails> Items, int Total, int Page, int PageSize,
    int IndexTotal, IReadOnlyList<DateTimeOffset> RecentCaptureTimes, SnapshotCaptureDistribution CaptureDistribution);
