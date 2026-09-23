using Archive.Core;
using Archive.Core.Storage;
using Archive.Worker;
using Archive.Worker.Capture;
using Archive.Worker.Import;
using Archive.Worker.Storage;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
var initializeDatabase = builder.Configuration.GetValue("Database:Initialize", false);
builder.Services.AddSingleton(ArchiveBrowserOptions.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton(WebBotAuthOptions.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton<WebBotAuthSigner>();
builder.Services.AddSingleton(CrawlPolicyOptions.FromConfiguration(builder.Configuration));
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

if (initializeDatabase)
{
    using var scope = host.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<DataContext>();
    await database.Database.MigrateAsync();
}

await host.RunAsync();
