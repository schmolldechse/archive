using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Archive.Core;
using Archive.Core.Storage;
using Microsoft.EntityFrameworkCore;
using Archive.SnapshotMigration;

var command = MigrationCommand.Parse(args);
if (command.Mode == MigrationMode.Help)
{
    Console.WriteLine(MigrationCommand.HelpText);
    return;
}

var builder = Host.CreateApplicationBuilder();
builder.Services.AddArchiveCore(builder.Configuration);
builder.Services.AddScoped<SnapshotResourceMigrator>();
using var host = builder.Build();

try
{
    await using var scope = host.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<DataContext>();
    await database.Database.OpenConnectionAsync();
    var lockAcquired = false;
    try
    {
        await using var lockCommand = database.Database.GetDbConnection().CreateCommand();
        lockCommand.CommandText = "SELECT pg_advisory_lock(741928360154)";
        await lockCommand.ExecuteScalarAsync();
        lockAcquired = true;

        var migrator = scope.ServiceProvider.GetRequiredService<SnapshotResourceMigrator>();
        var result = command.Mode switch
        {
            MigrationMode.DryRun => await migrator.MigrateAsync(apply: false, command.BatchSize, CancellationToken.None),
            MigrationMode.Apply => await migrator.MigrateAsync(apply: true, command.BatchSize, CancellationToken.None),
            MigrationMode.Rollback => await migrator.RollbackAsync(command.SnapshotId!.Value, CancellationToken.None),
            MigrationMode.Cleanup => await migrator.CleanupAsync(command.RetentionDays, command.ConfirmCleanup, CancellationToken.None),
            _ => throw new InvalidOperationException("Unknown migration command.")
        };
        Console.WriteLine(result.Summary);
        Environment.ExitCode = result.Failed == 0 ? 0 : 2;
    }
    finally
    {
        if (lockAcquired)
        {
            await using var unlockCommand = database.Database.GetDbConnection().CreateCommand();
            unlockCommand.CommandText = "SELECT pg_advisory_unlock(741928360154)";
            await unlockCommand.ExecuteScalarAsync();
        }
        await database.Database.CloseConnectionAsync();
    }
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Snapshot resource migration failed: {exception.Message}");
    Environment.ExitCode = 1;
}
