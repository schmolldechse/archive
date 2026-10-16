using Archive.Core.Processing;

namespace Archive.Worker.Processing;

public sealed class ArchiveWorkerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SnapshotProcessor _processor;
    private readonly ILogger<ArchiveWorkerService> _logger;

    internal ArchiveWorkerService(IServiceScopeFactory scopeFactory, SnapshotProcessor processor,
        ILogger<ArchiveWorkerService> logger)
    {
        _scopeFactory = scopeFactory;
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var lease = await ProcessingLease.AcquireAsync(_scopeFactory, stoppingToken);
                using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var monitor = lease.MonitorAsync(lifetime, _logger);
                try
                {
                    var interruptedUploads = await lease.RecoverInterruptedAsync(lifetime.Token);
                    foreach (var upload in interruptedUploads) await _processor.DeleteUploadAsync(upload, lifetime.Token);
                    while (!lifetime.IsCancellationRequested)
                    {
                        var claim = await lease.TryClaimNextAsync(Guid.NewGuid(), lifetime.Token);
                        if (claim is null) { await Task.Delay(500, lifetime.Token); continue; }
                        await _processor.ProcessAsync(claim, lifetime.Token);
                    }
                }
                finally { lifetime.Cancel(); await monitor; }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception) { _logger.LogError(exception, "Worker lease will be reacquired before recovery and new claims"); }
            try { await Task.Delay(1_000, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }
}
