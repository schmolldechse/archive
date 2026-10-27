using Archive.Core.Options;
using Archive.Core.Repositories.SnapshotDeletionRepository;

namespace Archive.Api.Processing;

/// <summary>Retries deletion of object-storage data after snapshot records have been removed.</summary>
public sealed class SnapshotDeletionCleanupService(
    IServiceScopeFactory scopes,
    ProcessingOptions options,
    ILogger<SnapshotDeletionCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ISnapshotDeletionRepository>()
                    .CleanupPendingAsync(options.CleanupBatchSize, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Stored objects for deleted snapshots could not be cleaned up");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(options.CleanupIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
