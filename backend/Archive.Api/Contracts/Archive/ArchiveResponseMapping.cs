using Archive.Core.Models.Archive;

namespace Archive.Api.Contracts.Archive;

internal static class ArchiveResponseMapping
{
    public static ArchiveResponse ToResponse(this ArchiveStatus archive) => new(
        archive.Id, archive.Status, archive.ProgressStep, archive.ProgressPercent, archive.Message,
        archive.QueuePosition, archive.SnapshotId, archive.SnapshotQuality, archive.ErrorMessage,
        archive.CreatedAt, archive.CompletedAt, archive.DurationMilliseconds, archive.StorageBytes, archive.ResourceCount);
}
