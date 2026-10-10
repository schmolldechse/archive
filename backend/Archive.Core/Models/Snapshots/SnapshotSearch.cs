using Archive.Core.Entities;

namespace Archive.Core.Models.Snapshots;

public sealed record SnapshotSearch(string? Text = null, string? SourceUrl = null, string? Title = null,
    string? Description = null, IReadOnlyList<string>? Tags = null, SourceType? SourceType = null,
    SnapshotQuality? Quality = null, DateTimeOffset? CapturedFrom = null, DateTimeOffset? CapturedUntil = null,
    SnapshotSortOrder Order = SnapshotSortOrder.Desc, int Page = 1, int PageSize = 24);
