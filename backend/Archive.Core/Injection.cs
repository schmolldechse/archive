using Archive.Core.Entities;
using Archive.Core.Uploads;
using Archive.Core.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Archive.Core.Repositories.UserRepository;
using Archive.Core.Repositories.AuthenticationRepository;
using Archive.Core.Repositories.ArchiveRepository;
using Archive.Core.Repositories.SnapshotRepository;
using Archive.Core.Repositories.ProfileImageRepository;
using Archive.Core.Entities.Processing;

namespace Archive.Core;

public static class Injection
{
    public static IServiceCollection AddArchiveCore(this IServiceCollection services, IConfiguration configuration,
        bool initializeDatabase = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ConnectionStringOptions>()
            .Bind(configuration.GetSection(ConnectionStringOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<DatabaseOptions>()
            .Configure(options => options.Initialize = initializeDatabase)
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<DatabaseOptions>>().Value);
        services.AddOptions<ArchiveOptions>()
            .Bind(configuration.GetSection(ArchiveOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<ArchiveOptions>>().Value);
        services.AddOptions<ObjectStorageOptions>()
            .Bind(configuration.GetSection(ObjectStorageOptions.SectionName))
            .PostConfigure(options =>
            {
                options.AccessKey = options.AccessKey?.Trim();
                options.SecretKey = options.SecretKey?.Trim();
            })
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<ObjectStorageOptions>>().Value);

        services.AddDbContext<DataContext>((provider, options) => options.UseNpgsql(
            provider.GetRequiredService<IOptions<ConnectionStringOptions>>().Value.ArchiveDatabase,
            options =>
            {
                options.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName,
                    "public");
                options.MapEnum<SourceType>("source_type", "types");
                options.MapEnum<SnapshotQuality>("snapshot_quality", "types");
                options.MapEnum<ProcessingStatus>("processing_status", "types");
                options.MapEnum<ProgressStep>("progress_step", "types");
            }));
        services.AddSingleton<IUploadFormatProbe, HtmlUploadFormatProbe>();
        services.AddSingleton<IUploadFormatProbe, MhtmlUploadFormatProbe>();
        services.AddSingleton<IUploadFormatProbe, WebArchiveUploadFormatProbe>();
        services.AddSingleton<UploadFormatSelector>();

        services.AddScoped<IAuthenticationRepository, AuthenticationRepository>()
            .AddScoped<IUserRepository, UserRepository>()
            .AddScoped<IArchiveRepository, ArchiveRepository>()
            .AddScoped<ISnapshotRepository, SnapshotRepository>()
            .AddScoped<IProfileImageRepository, ProfileImageRepository>();
        return services;
    }
}
