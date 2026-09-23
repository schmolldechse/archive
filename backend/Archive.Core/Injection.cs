using Archive.Core.Entities;
using Archive.Core.Uploads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Archive.Core;

public static class Injection
{
    public static IServiceCollection AddArchiveCore(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("ArchiveDatabase")
            ?? throw new InvalidOperationException("The ArchiveDatabase connection string is required.");

        services.AddDbContext<DataContext>(options => options.UseNpgsql(
            connectionString,
            options =>
            {
                options.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName,
                    "public");
                options.MapEnum<SourceType>("source_type", "types");
                options.MapEnum<JobStatus>("job_status", "types");
                options.MapEnum<ProgressStep>("progress_step", "types");
                options.MapEnum<SnapshotQuality>("snapshot_quality", "types");
            }));
        services.AddSingleton<IUploadFormatProbe, HtmlUploadFormatProbe>();
        services.AddSingleton<IUploadFormatProbe, MhtmlUploadFormatProbe>();
        services.AddSingleton<IUploadFormatProbe, WebArchiveUploadFormatProbe>();
        services.AddSingleton<UploadFormatSelector>();
        return services;
    }
}
