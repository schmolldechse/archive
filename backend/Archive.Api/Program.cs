using Archive.Api.OpenApi;
using Archive.Api.Storage;
using Archive.Core;
using Archive.Core.Storage;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json;
using System.Text.Json.Serialization;

if (args is ["--healthcheck"])
{
    Environment.ExitCode = await CheckHealthAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);

var maxUploadBytes = builder.Configuration.GetValue("Archive:MaxUploadBytes", 250_000_000L);
var maxUploadRequestBytes = checked(maxUploadBytes + 1_000_000L); // Multipart fields and boundaries sit outside the file limit.
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maxUploadBytes);

builder.Services.AddOpenApi(options =>
    options.AddOperationTransformer<ArchiveRequestBodyOperationTransformer>());
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(null, allowIntegerValues: false)));
builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(null, allowIntegerValues: false)));
builder.Services.AddArchiveCore(builder.Configuration);
builder.Services.AddSingleton<IArchiveObjectStore, S3ArchiveObjectStore>();
builder.Services.AddCors(options => options.AddPolicy("public", policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Configuration.GetValue("Database:Initialize", true))
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<DataContext>();
    await database.Database.MigrateAsync();
}

app.UseCors("public");
app.Use(async (context, next) =>
{
    if (HttpMethods.IsPost(context.Request.Method) &&
        string.Equals(context.Request.Path.Value, "/api/archive", StringComparison.OrdinalIgnoreCase) &&
        context.Request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true)
    {
        var bodyLimit = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodyLimit is { IsReadOnly: false })
            bodyLimit.MaxRequestBodySize = maxUploadRequestBytes;
    }

    await next(context);
});
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await app.RunAsync();

static async Task<int> CheckHealthAsync()
{
    try
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        using var response = await client.GetAsync("http://127.0.0.1:8080/healthz");
        return response.IsSuccessStatusCode ? 0 : 1;
    }
    catch
    {
        return 1;
    }
}
