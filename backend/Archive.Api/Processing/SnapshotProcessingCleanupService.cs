using Archive.Core.Options;
using Archive.Core.Repositories.SnapshotProcessingRepository;

namespace Archive.Api.Processing;

public sealed class SnapshotProcessingCleanupService(IServiceScopeFactory scopes, ProcessingOptions options,
    ILogger<SnapshotProcessingCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ISnapshotProcessingRepository>()
                    .CleanupExpiredAsync(options.CleanupBatchSize, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception) { logger.LogError(exception, "Expired snapshot processing could not be cleaned up"); }
            try { await Task.Delay(TimeSpan.FromSeconds(options.CleanupIntervalSeconds), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }
}
