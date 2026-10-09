using Archive.Core;
using Archive.Core.Options;
using Archive.Core.Storage;
using Archive.Worker;
using Archive.Worker.Capture;
using Archive.Worker.Import;
using Archive.Worker.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOptions<ArchiveBrowserOptions>()
    .Bind(builder.Configuration.GetSection(ArchiveBrowserOptions.SectionName))
    .PostConfigure(options => options.UserAgent = options.UserAgent?.Trim() ?? "")
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton(provider => provider.GetRequiredService<IOptions<ArchiveBrowserOptions>>().Value);
builder.Services.AddOptions<WebBotAuthOptions>()
    .Bind(builder.Configuration.GetSection(WebBotAuthOptions.SectionName))
    .PostConfigure(options =>
    {
        options.PrivateKeyPath = options.PrivateKeyPath?.Trim();
        options.SignatureAgent = options.SignatureAgent?.Trim();
    })
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton(provider => provider.GetRequiredService<IOptions<WebBotAuthOptions>>().Value);
builder.Services.AddSingleton<WebBotAuthSigner>();
builder.Services.AddOptions<CrawlPolicyOptions>()
    .Bind(builder.Configuration.GetSection(CrawlPolicyOptions.SectionName))
    .PostConfigure(options => options.UserAgentToken = options.UserAgentToken?.Trim() ?? "")
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton(provider => provider.GetRequiredService<IOptions<CrawlPolicyOptions>>().Value);
builder.Services.AddSingleton<HostRateLimiter>();
builder.Services.AddSingleton<CrawlPolicyService>();
builder.Services.AddArchiveCore(builder.Configuration);
builder.Services.AddSingleton<IArchiveObjectStore, S3ArchiveObjectStore>();
builder.Services.AddSingleton<BrowserCaptureEngine>();
builder.Services.AddSingleton<IUploadedDocumentDecoder, HtmlDecoder>();
builder.Services.AddSingleton<IUploadedDocumentDecoder, MhtmlDecoder>();
builder.Services.AddSingleton<IUploadedDocumentDecoder, WebArchiveDecoder>();
builder.Services.AddSingleton<OfflineImportEngine>();
builder.Services.AddHostedService<ArchiveWorkerService>();

var host = builder.Build();

host.Services.GetRequiredService<IStartupValidator>().Validate();
if (host.Services.GetRequiredService<DatabaseOptions>().Initialize)
{
    using var scope = host.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<DataContext>();
    await database.Database.MigrateAsync();
}

await host.RunAsync();
