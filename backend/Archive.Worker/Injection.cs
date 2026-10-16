using Archive.Worker.Capture.Browser;
using Archive.Worker.Capture.Crawling;
using Archive.Worker.Capture.Network;
using Archive.Worker.Import;
using Archive.Worker.Import.Decoders;
using Archive.Worker.Processing;
using Microsoft.Extensions.Options;

namespace Archive.Worker;

public static class Injection
{
    public static IServiceCollection AddArchiveWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ArchiveBrowserOptions>()
            .Bind(configuration.GetSection(ArchiveBrowserOptions.SectionName))
            .PostConfigure(options => options.UserAgent = options.UserAgent?.Trim() ?? "")
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<ArchiveBrowserOptions>>().Value);
        services.AddOptions<WebBotAuthOptions>()
            .Bind(configuration.GetSection(WebBotAuthOptions.SectionName))
            .PostConfigure(options =>
            {
                options.PrivateKeyPath = options.PrivateKeyPath?.Trim();
                options.SignatureAgent = options.SignatureAgent?.Trim();
            })
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<WebBotAuthOptions>>().Value);
        services.AddSingleton<WebBotAuthSigner>();
        services.AddOptions<CrawlPolicyOptions>()
            .Bind(configuration.GetSection(CrawlPolicyOptions.SectionName))
            .PostConfigure(options => options.UserAgentToken = options.UserAgentToken?.Trim() ?? "")
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<CrawlPolicyOptions>>().Value);
        services.AddSingleton<HostRateLimiter>();
        services.AddSingleton<CrawlPolicyService>();
        services.AddSingleton<BrowserCaptureEngine>();
        services.AddSingleton<IUploadedDocumentDecoder, HtmlDecoder>();
        services.AddSingleton<IUploadedDocumentDecoder, MhtmlDecoder>();
        services.AddSingleton<IUploadedDocumentDecoder, WebArchiveDecoder>();
        services.AddSingleton<OfflineImportEngine>();
        services.AddSingleton<SnapshotProcessor>();
        services.AddHostedService(provider => new ArchiveWorkerService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<SnapshotProcessor>(),
            provider.GetRequiredService<ILogger<ArchiveWorkerService>>()));
        return services;
    }
}
