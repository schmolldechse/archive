using Archive.Core.Entities.Processing;
using Archive.Core.Models.Processing;

namespace Archive.Core.Repositories.SnapshotProcessingRepository;

public interface ISnapshotProcessingRepository
{
    Task<SnapshotProcessingState?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProcessingCancellation> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Called through ProcessingLease on the scope holding the advisory lock.</summary>
    Task<ClaimedSnapshot?> TryClaimNextAsync(Guid owner, CancellationToken cancellationToken = default);
    Task<bool> CanContinueAsync(Guid id, Guid owner, CancellationToken cancellationToken = default);
    Task<bool> UpdateStepAsync(Guid id, Guid owner, ProgressStep step, CancellationToken cancellationToken = default);
    Task<bool> PublishAsync(Guid id, Guid owner, SnapshotPublication result, CancellationToken cancellationToken = default);
    Task<ProcessingCompletion> FinishAsync(Guid id, Guid owner, ProcessingStatus status, CancellationToken cancellationToken = default);
    /// <summary>Called through ProcessingLease; returns uploads to delete after leaving the database gate.</summary>
    Task<IReadOnlyList<string>> RecoverInterruptedAsync(CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredAsync(int batchSize, CancellationToken cancellationToken = default);
}
