using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Archive.Core.Models.Processing;
using Archive.Core.Repositories.SnapshotProcessingRepository;

namespace Archive.Core.Processing;

// Claim and recovery use the very session holding this lock. A lost session is
// never transparently replaced while retaining the right to start more captures.
public sealed class ProcessingLease : IAsyncDisposable
{
    public const long WorkerKey = 0x4152434849564501L;
    private readonly AsyncServiceScope _scope;
    private readonly DataContext _database;
    private readonly ISnapshotProcessingRepository _repository;
    private readonly DbConnection _connection;
    private readonly SemaphoreSlim _gate = new(1);
    private bool _invalid;

    private ProcessingLease(AsyncServiceScope scope, DataContext database)
    {
        _scope = scope;
        _database = database;
        // Both resolutions belong to this scope, so claim/recovery use the lock-holding DbContext.
        _repository = scope.ServiceProvider.GetRequiredService<ISnapshotProcessingRepository>();
        _connection = database.Database.GetDbConnection();
    }

    public static async Task<ProcessingLease> AcquireAsync(IServiceScopeFactory scopeFactory, CancellationToken token)
    {
        var scope = scopeFactory.CreateAsyncScope();
        ProcessingLease? lease = null;
        try
        {
            var database = scope.ServiceProvider.GetRequiredService<DataContext>();
            lease = new ProcessingLease(scope, database);
            await lease._connection.OpenAsync(token);
            await lease.CommandAsync("SELECT pg_advisory_lock(@key)", token, timeout: 0);
            return lease;
        }
        catch
        {
            if (lease is null) await scope.DisposeAsync();
            else await lease.DisposeAsync();
            throw;
        }
    }

    public Task<ClaimedSnapshot?> TryClaimNextAsync(Guid owner, CancellationToken token = default) =>
        ExecuteAsync(ct => _repository.TryClaimNextAsync(owner, ct), token);

    public Task<IReadOnlyList<string>> RecoverInterruptedAsync(CancellationToken token = default) =>
        ExecuteAsync(ct => _repository.RecoverInterruptedAsync(ct), token);

    private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            if (_invalid || _connection.State != ConnectionState.Open)
                throw new ProcessingUnavailableException("The worker lease session has been lost.");
            _database.ChangeTracker.Clear();
            return await action(token);
        }
        catch
        {
            // Even an ambiguous claim must reacquire the lease and recover before
            // another claim. The engine has not been started without confirmation.
            _invalid = true;
            throw;
        }
        finally { _database.ChangeTracker.Clear(); _gate.Release(); }
    }

    public async Task MonitorAsync(CancellationTokenSource lifetime, ILogger logger)
    {
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), lifetime.Token);
                await ExecuteAsync(async token =>
                {
                    await CommandAsync("SELECT 1", token, timeout: 5);
                    return true;
                }, lifetime.Token);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "PostgreSQL worker lease lost; stopping the active capture");
            lifetime.Cancel();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_connection.State == ConnectionState.Open)
                await CommandAsync("SELECT pg_advisory_unlock(@key)", CancellationToken.None, timeout: 5);
        }
        catch
        {
            // Do not return a session with an uncertain lock to the pool.
            if (_connection is NpgsqlConnection connection) NpgsqlConnection.ClearPool(connection);
        }
        finally
        {
            await _connection.CloseAsync();
            await _scope.DisposeAsync();
            _gate.Release();
            _gate.Dispose();
        }
    }

    private async Task CommandAsync(string sql, CancellationToken token, int timeout)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = timeout;
        if (sql.Contains("@key", StringComparison.Ordinal))
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "key";
            parameter.Value = WorkerKey;
            command.Parameters.Add(parameter);
        }
        await command.ExecuteScalarAsync(token);
    }
}
