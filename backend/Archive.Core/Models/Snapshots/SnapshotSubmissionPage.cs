namespace Archive.Core.Models.Snapshots;

/// <summary>Shared pagination for bounded private records.</summary>
public sealed record SnapshotSubmissionPage(IReadOnlyList<SnapshotSubmissionSummary> Items, int Total, int Page, int PageSize);
