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
    /// <summary>Opens public content after authorizing the requested resource through the snapshot manifest.</summary>
    Task<SnapshotContentResult> OpenContentAsync(Guid snapshotId, string? path, SnapshotContentRequest? request = null,
        CancellationToken cancellationToken = default);
    /// <summary>Opens manifest-authorized content after the API has authorized private snapshot access.</summary>
    Task<SnapshotContentResult> OpenAuthorizedContentAsync(Guid snapshotId, string? path, Guid subjectUserId,
        SnapshotContentRequest? request = null,
        CancellationToken cancellationToken = default);
    Task<ArchiveObject?> OpenScreenshotAsync(Guid snapshotId, CancellationToken cancellationToken = default);
    /// <summary>Checks public, owner, or permission-based access to processing state without disclosing privacy.</summary>
    Task<bool> CanReadProcessingAsync(Guid snapshotId, Guid? subjectUserId,
        CancellationToken cancellationToken = default);
    /// <summary>Checks whether the public workflow or a private owner may cancel processing.</summary>
    Task<bool> CanCancelProcessingAsync(Guid snapshotId, Guid? subjectUserId,
        CancellationToken cancellationToken = default);
}
