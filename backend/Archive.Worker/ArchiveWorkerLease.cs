using Archive.Core;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Archive.Worker;

// Startup recovery changes global queue state. A PostgreSQL session lock keeps
// exactly one active worker while allowing additional instances to wait as warm
// standbys; losing the database session releases the lease automatically.
internal sealed class ArchiveWorkerLease : IAsyncDisposable
{
    private const long LeaseKey = 0x4152434849564501L;
    private readonly AsyncServiceScope _scope;
    private readonly DbConnection _connection;

    private ArchiveWorkerLease(AsyncServiceScope scope, DbConnection connection)
    {
        _scope = scope;
        _connection = connection;
    }

    public static async Task<ArchiveWorkerLease> AcquireAsync(IServiceScopeFactory scopeFactory,
        CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var database = scope.ServiceProvider.GetRequiredService<DataContext>();
            var connection = database.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);
            await ExecuteLockCommandAsync(connection, "SELECT pg_advisory_lock(@key)", cancellationToken,
                commandTimeoutSeconds: 0);
            return new ArchiveWorkerLease(scope, connection);
        }
        catch
        {
            await scope.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                await ExecuteLockCommandAsync(_connection, "SELECT pg_advisory_unlock(@key)", CancellationToken.None,
                    commandTimeoutSeconds: 5);
        }
        catch
        {
            // A broken/closed PostgreSQL session has already released its locks.
        }
        finally
        {
            await _connection.CloseAsync();
            await _scope.DisposeAsync();
        }
    }

    public async Task MonitorAsync(CancellationTokenSource workerLifetime, ILogger logger)
    {
        try
        {
            while (!workerLifetime.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), workerLifetime.Token);
                await ExecuteLockCommandAsync(_connection, "SELECT 1", workerLifetime.Token, commandTimeoutSeconds: 5);
            }
        }
        catch (OperationCanceledException) when (workerLifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "PostgreSQL-Worker-Lease verloren; aktive Aufnahme wird beendet");
            workerLifetime.Cancel();
        }
    }

    private static async Task ExecuteLockCommandAsync(DbConnection connection, string sql,
        CancellationToken cancellationToken, int? commandTimeoutSeconds = null)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (commandTimeoutSeconds is not null)
            command.CommandTimeout = commandTimeoutSeconds.Value;
        if (sql.Contains("@key", StringComparison.Ordinal))
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "key";
            parameter.Value = LeaseKey;
            command.Parameters.Add(parameter);
        }
        await command.ExecuteScalarAsync(cancellationToken);
    }
}
