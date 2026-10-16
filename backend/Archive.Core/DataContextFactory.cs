using Archive.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Archive.Core.Entities.Processing;

namespace Archive.Core;

public sealed class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=archive;Username=archive;Password=password",
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                npgsql.MapEnum<SourceType>("source_type", "types");
                npgsql.MapEnum<SnapshotQuality>("snapshot_quality", "types");
                npgsql.MapEnum<ProcessingStatus>("processing_status", "types");
                npgsql.MapEnum<ProgressStep>("progress_step", "types");
            });
        return new DataContext(optionsBuilder.Options);
    }
}
