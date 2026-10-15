using Archive.Core.Models.Snapshots;
using Archive.Core.Storage;

namespace Archive.Core.Repositories.SnapshotRepository;

public interface ISnapshotRepository
{
    Task<SnapshotCreationResult> CreateFromUrlAsync(SnapshotCreationRequest request, CancellationToken cancellationToken = default);
    Task<SnapshotCreationResult> CreateFromFileAsync(SnapshotCreationRequest request, SnapshotUpload upload, CancellationToken cancellationToken = default);
    Task<SnapshotSearchResult> SearchAsync(SnapshotSearch search, CancellationToken cancellationToken = default);
    Task<SnapshotDetails?> GetByIdAsync(Guid snapshotId, CancellationToken cancellationToken = default);
    Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path, CancellationToken cancellationToken = default);
    Task<ArchiveObject?> OpenScreenshotAsync(Guid snapshotId, CancellationToken cancellationToken = default);
}
