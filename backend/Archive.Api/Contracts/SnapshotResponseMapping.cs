using Archive.Core.Entities;
using Archive.Core.Models.Snapshots;

namespace Archive.Api.Contracts;

internal static class SnapshotResponseMapping
{
    public static SnapshotResponse ToResponse(this SnapshotDetails snapshot, string publicBaseUrl) => new(
        snapshot.Id,
        snapshot.SourceType == SourceType.Url ? snapshot.OriginUrl : null,
        snapshot.SourceType != SourceType.Url ? snapshot.OriginUrl : null,
        snapshot.Title, snapshot.Description, snapshot.Quality,
        snapshot.CreatedAt, snapshot.DurationMilliseconds, snapshot.StorageBytes, snapshot.ResourceCount,
        snapshot.Tags,
        $"{publicBaseUrl.TrimEnd('/')}/api/snapshots/{snapshot.Id}/screenshot",
        $"{publicBaseUrl.TrimEnd('/')}/api/snapshots/{snapshot.Id}/content/index.html");

    public static SnapshotListResponse ToResponse(this SnapshotPage page, string publicBaseUrl) => new(
        page.Items.Select(item => item.ToResponse(publicBaseUrl)).ToArray(), page.Total, page.Page, page.PageSize,
        page.IndexTotal, page.RecentCaptureTimes, new(page.CaptureDistribution.Total, page.CaptureDistribution.FirstYear,
            page.CaptureDistribution.LastYear, page.CaptureDistribution.Years.Select(year => new CaptureYearResponse(year.Year, year.Count)).ToArray()));
}
