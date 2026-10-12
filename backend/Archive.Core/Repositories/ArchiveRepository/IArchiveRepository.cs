using Archive.Core.Models.Archive;

namespace Archive.Core.Repositories.ArchiveRepository;

public interface IArchiveRepository
{
    Task<ArchiveCreationResult> CreateFromUrlAsync(ArchiveCreationRequest request, CancellationToken cancellationToken = default);
    Task<ArchiveCreationResult> CreateFromFileAsync(ArchiveCreationRequest request, ArchiveUpload upload, CancellationToken cancellationToken = default);
    Task<ArchiveStatus?> GetByIdAsync(Guid archiveId, CancellationToken cancellationToken = default);
    Task<ArchiveCancellationResult> CancelAsync(Guid archiveId, CancellationToken cancellationToken = default);
}
