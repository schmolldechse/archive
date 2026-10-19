namespace Archive.Core.Models.Snapshots;

/// <summary>History pagination counts only events visible to the current reader.</summary>
public sealed record SnapshotHistoryPage(IReadOnlyList<SnapshotHistoryItem> Items, int Total, int Page, int PageSize);
