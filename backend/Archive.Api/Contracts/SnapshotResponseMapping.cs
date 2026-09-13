using Archive.Core.Entities;

namespace Archive.Api.Contracts;

internal static class SnapshotResponseMapping
{
    public static SnapshotResponse ToResponse(this Snapshot snapshot, string publicBaseUrl) => new(
        snapshot.Id,
        snapshot.SourceType == SourceType.Url ? snapshot.OriginUrl : null,
        snapshot.SourceType == SourceType.HtmlFile ? snapshot.OriginUrl : null,
        snapshot.Title, snapshot.Description, snapshot.Quality,
        snapshot.CreatedAt, snapshot.DurationMilliseconds, snapshot.StorageBytes, snapshot.ResourceCount,
        snapshot.SnapshotTags.Select(x => x.Tag.Value).Order().ToArray(),
        $"{publicBaseUrl.TrimEnd('/')}/api/snapshots/{snapshot.Id}/screenshot",
        $"{publicBaseUrl.TrimEnd('/')}/api/snapshots/{snapshot.Id}/content/index.html");
}
