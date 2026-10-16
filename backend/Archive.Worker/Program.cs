using Archive.Core;
using Archive.Core.Options;
using Archive.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddArchiveCore(builder.Configuration);
builder.Services.AddArchiveWorker(builder.Configuration);

var host = builder.Build();

host.Services.GetRequiredService<IStartupValidator>().Validate();
if (host.Services.GetRequiredService<DatabaseOptions>().Initialize)
{
    using var scope = host.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<DataContext>();
    await database.Database.MigrateAsync();
}

await host.RunAsync();
