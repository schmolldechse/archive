using Amazon.S3;
using Archive.Core.Entities;
using Archive.Core.Entities.Authentication.Permissible;
using Archive.Core.Entities.Processing;
using Archive.Core.Options;
using Archive.Core.Repositories.AuthenticationRepository;
using Archive.Core.Repositories.ProfileImageRepository;
using Archive.Core.Repositories.SnapshotProcessingRepository;
using Archive.Core.Repositories.SnapshotRepository;
using Archive.Core.Repositories.UserRepository;
using Archive.Core.Repositories.SnapshotSubmissionRepository;
using Archive.Core.Repositories.SnapshotHistoryRepository;
using Archive.Core.Storage;
using Archive.Core.Uploads;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Archive.Core;

public static class Injection
{
    public static IServiceCollection AddArchiveCore(
        this IServiceCollection services,
        IConfiguration configuration,
        bool initializeDatabase = false
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

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
        services.AddOptions<ProcessingOptions>().Bind(configuration.GetSection(ProcessingOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<ProcessingOptions>>().Value);

        services.AddSingleton<IAmazonS3>(provider =>
        {
            var options = provider.GetRequiredService<ObjectStorageOptions>();
            var config = new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                ForcePathStyle = true
            };

            return string.IsNullOrWhiteSpace(options.AccessKey)
                ? new AmazonS3Client(config)
                : new AmazonS3Client(options.AccessKey, options.SecretKey, config);
        });
        services.AddSingleton<IArchiveObjectStore, S3ArchiveObjectStore>();

        services.AddDbContext<DataContext>((provider, options) => options.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection"),
            options =>
            {
                options.MigrationsHistoryTable("__EFMigrationsHistory", "public");

                options.MapEnum<SourceType>("source_type", "types");
                options.MapEnum<SnapshotQuality>("snapshot_quality", "types");
                options.MapEnum<SnapshotModerationStatus>("snapshot_moderation_status", "types");

                options.MapEnum<ProcessingStatus>("processing_status", "types");
                options.MapEnum<ProgressStep>("progress_step", "types");

                options.MapEnum<ArchiveRole>("role", "types");
                options.MapEnum<ArchivePermissionEffect>("permission_effect", "types");
            }));

        services.AddSingleton<IUploadFormatProbe, HtmlUploadFormatProbe>();
        services.AddSingleton<IUploadFormatProbe, MhtmlUploadFormatProbe>();
        services.AddSingleton<IUploadFormatProbe, WebArchiveUploadFormatProbe>();
        services.AddSingleton<UploadFormatSelector>();

        services.AddScoped<IAuthenticationRepository, AuthenticationRepository>()
            .AddScoped<IUserRepository, UserRepository>()
            .AddScoped<ISnapshotRepository, SnapshotRepository>()
            .AddScoped<SnapshotManifestReader>()
            .AddScoped<ISnapshotProcessingRepository, SnapshotProcessingRepository>()
            .AddScoped<ISnapshotSubmissionRepository, SnapshotSubmissionRepository>()
            .AddScoped<ISnapshotHistoryRepository, SnapshotHistoryRepository>()
            .AddScoped<IProfileImageRepository, ProfileImageRepository>();
        return services;
    }
}
