using Archive.Core.Entities;
using Archive.Core.Entities.Processing;
using Archive.Core.Processing;
using Archive.Core.Models.Processing;
using Archive.Core.Repositories.SnapshotProcessingRepository;
using Archive.Core.Storage;
using Archive.Worker.Capture;
using Archive.Worker.Import;

namespace Archive.Worker;

public sealed class ArchiveWorkerService(IServiceScopeFactory scopeFactory,
    BrowserCaptureEngine captureEngine, OfflineImportEngine offlineImportEngine,
    IArchiveObjectStore objectStore, ILogger<ArchiveWorkerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var lease = await ProcessingLease.AcquireAsync(scopeFactory, stoppingToken);
                using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var monitor = lease.MonitorAsync(lifetime, logger);
                try
                {
                    var interruptedUploads = await lease.RecoverInterruptedAsync(lifetime.Token);
                    foreach (var upload in interruptedUploads) await DeleteUploadAsync(upload, lifetime.Token);
                    while (!lifetime.IsCancellationRequested)
                    {
                        var claim = await lease.TryClaimNextAsync(Guid.NewGuid(), lifetime.Token);
                        if (claim is null) { await Task.Delay(500, lifetime.Token); continue; }
                        await ProcessAsync(claim, lifetime.Token);
                    }
                }
                finally { lifetime.Cancel(); await monitor; }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception) { logger.LogError(exception, "Worker lease will be reacquired before recovery and new claims"); }
            try { await Task.Delay(1_000, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }

    private async Task ProcessAsync(ClaimedSnapshot claim, CancellationToken token)
    {
        var input = claim.Input;
        CaptureResult? capture = null;
        var status = ProcessingStatus.Failed;
        var completion = ProcessingCompletion.Unpublished;
        try
        {
            var progress = new CaptureProgress(scopeFactory, input.Id, claim.OwnerToken, logger, token);
            try { capture = await CaptureAsync(claim, progress, token); }
            finally { await progress.FlushAsync(); }
            var published = await WithRepositoryAsync(processing => processing.PublishAsync(input.Id, claim.OwnerToken,
                new(capture.Quality, capture.ContentPrefix, capture.DurationMilliseconds, capture.StorageBytes, capture.ResourceCount), token));
            if (!published) throw new CaptureCancelledException();
            completion = ProcessingCompletion.Published;
        }
        catch (CaptureCancelledException) { status = ProcessingStatus.Cancelled; }
        catch (CaptureDiscardedException exception)
        {
            status = ProcessingStatus.Discarded;
            logger.LogWarning(exception, "Snapshot {SnapshotId} exceeded a capture limit", input.Id);
        }
        catch (CaptureFailedException exception)
        {
            logger.LogWarning(exception, "Snapshot {SnapshotId} capture failed", input.Id);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // The next lease reconciles the attempt. Do not delete uncertain results.
            return;
        }
        catch (Exception exception) { logger.LogError(exception, "Snapshot {SnapshotId} processing failed", input.Id); }

        if (completion != ProcessingCompletion.Published)
            completion = await FinishUntilConfirmedAsync(claim, status, token);
        if (completion == ProcessingCompletion.OwnershipLost) return;
        await DeleteUploadAsync(input.UploadedObjectKey, token);
        if (completion == ProcessingCompletion.Unpublished && capture is not null)
            foreach (var key in capture.StoredObjectKeys.Reverse())
            {
                try { await objectStore.DeleteAsync(key, token); }
                catch (Exception exception) { logger.LogWarning(exception, "Unpublished capture object could not be removed"); }
            }
    }

    private async Task<ProcessingCompletion> FinishUntilConfirmedAsync(ClaimedSnapshot claim, ProcessingStatus status,
        CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            try { return await WithRepositoryAsync(processing => processing.FinishAsync(claim.Input.Id, claim.OwnerToken, status, token)); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Snapshot {SnapshotId} finalization will be reconciled", claim.Input.Id);
                await Task.Delay(1_000, token);
            }
        }
    }

    private async Task<CaptureResult> CaptureAsync(ClaimedSnapshot claim, IProgress<ProgressStep> progress, CancellationToken token)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        var capture = claim.Input.SourceType == SourceType.Url
            ? captureEngine.CaptureAsync(claim.Input, progress, cancellation.Token)
            : offlineImportEngine.CaptureAsync(claim.Input, progress, cancellation.Token);
        try
        {
            while (!capture.IsCompleted)
            {
                await Task.WhenAny(capture, Task.Delay(250, token));
                if (capture.IsCompleted) break;
                token.ThrowIfCancellationRequested();
                if (!await WithRepositoryAsync(processing => processing.CanContinueAsync(claim.Input.Id, claim.OwnerToken, token)))
                { cancellation.Cancel(); break; }
            }
            return await capture;
        }
        catch
        {
            cancellation.Cancel();
            try { await capture; } catch { }
            throw;
        }
    }

    private async Task DeleteUploadAsync(string? key, CancellationToken token)
    {
        if (key is null) return;
        try { await objectStore.DeleteAsync(key, token); }
        catch (Exception exception) { logger.LogWarning(exception, "Temporary upload {ObjectKey} could not be removed", key); }
    }

    private async Task<T> WithRepositoryAsync<T>(Func<ISnapshotProcessingRepository, Task<T>> action)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<ISnapshotProcessingRepository>());
    }

    private sealed class CaptureProgress(IServiceScopeFactory scopes, Guid id, Guid owner,
        ILogger logger, CancellationToken token) : IProgress<ProgressStep>
    {
        private readonly object _gate = new();
        private Task _pending = Task.CompletedTask;
        public void Report(ProgressStep step)
        {
            lock (_gate) _pending = SaveAsync(_pending, step);
        }
        private async Task SaveAsync(Task previous, ProgressStep step)
        {
            await previous;
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ISnapshotProcessingRepository>().UpdateStepAsync(id, owner, step, token);
            }
            catch (Exception exception) { logger.LogWarning(exception, "Phase update for snapshot {SnapshotId} could not be saved", id); }
        }
        public Task FlushAsync() { lock (_gate) return _pending; }
    }
}
