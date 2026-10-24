using Archive.Core.Models.Snapshots;
using Archive.Core.Storage;

namespace Archive.Core.Repositories.SnapshotRepository;

public interface ISnapshotRepository
{
    Task<SnapshotCreationResult> CreateFromUrlAsync(SnapshotCreationRequest request, CancellationToken cancellationToken = default);
    Task<SnapshotCreationResult> CreateFromFileAsync(SnapshotCreationRequest request, SnapshotUpload upload, CancellationToken cancellationToken = default);
    Task<SnapshotSearchResult> SearchAsync(SnapshotSearch search, CancellationToken cancellationToken = default);
    Task<SnapshotDetails?> GetByIdAsync(Guid snapshotId, CancellationToken cancellationToken = default);
    Task<SnapshotHistoryPage?> HistoryAsync(Guid snapshotId, int page, int pageSize,
        CancellationToken cancellationToken = default);
    /// <summary>Opens public content, enforcing the replay manifest when delivery uses the credential-free origin.</summary>
    Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path, bool replayDelivery,
        CancellationToken cancellationToken = default);
    Task<ArchiveObject?> OpenScreenshotAsync(Guid snapshotId, CancellationToken cancellationToken = default);
}
