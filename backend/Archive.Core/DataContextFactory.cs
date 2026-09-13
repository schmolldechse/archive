using Archive.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.Json;

namespace Archive.Core;

public sealed class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var connectionString = FindConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName,
                    "public");
                npgsql.MapEnum<SourceType>("source_type", "types");
                npgsql.MapEnum<JobStatus>("job_status", "types");
                npgsql.MapEnum<ProgressStep>("progress_step", "types");
                npgsql.MapEnum<SnapshotQuality>("snapshot_quality", "types");
            });
        return new DataContext(optionsBuilder.Options);
    }

    private static string FindConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("ConnectionStrings__ArchiveDatabase") ??
            Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var workingDirectory = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(workingDirectory, "Archive.Api", "appsettings.Development.json"),
            Path.Combine(workingDirectory, "backend", "Archive.Api", "appsettings.Development.json"),
            Path.Combine(workingDirectory, "..", "Archive.Api", "appsettings.Development.json")
        };

        foreach (var candidate in candidates.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(candidate))
                continue;

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(candidate));
                if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings) &&
                    connectionStrings.TryGetProperty("ArchiveDatabase", out var value) &&
                    !string.IsNullOrWhiteSpace(value.GetString()))
                    return value.GetString()!;
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"The local development configuration '{candidate}' is not valid JSON.", exception);
            }
        }

        throw new InvalidOperationException(
            "Configure ConnectionStrings__ArchiveDatabase, POSTGRES_CONNECTION, or a local Archive.Api/appsettings.Development.json file before using Entity Framework tools.");
    }
}
