using Archive.Core;
using Archive.SnapshotMigrator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

if (!SnapshotMigratorOptions.TryParse(args, out var options, out var error))
{
    if (!string.IsNullOrWhiteSpace(error)) Console.Error.WriteLine(error);
    Console.WriteLine(SnapshotMigratorOptions.Help);
    return string.IsNullOrEmpty(error) ? 0 : 2;
}

try
{
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = [] });
    builder.Services.AddArchiveCore(builder.Configuration);
    builder.Services.AddScoped<SnapshotMigrationService>();

    using var host = builder.Build();
    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellation.Cancel();
    };

    await using var scope = host.Services.CreateAsyncScope();
    var migrator = scope.ServiceProvider.GetRequiredService<SnapshotMigrationService>();
    return await migrator.ExecuteAsync(options!, cancellation.Token);
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Snapshot migration was cancelled.");
    return 130;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Snapshot migration failed: {exception.Message}");
    return 1;
}
