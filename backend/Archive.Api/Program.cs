using Archive.Api.Authentication;
using Archive.Api.ExceptionHandling;
using Archive.Api.OpenApi;
using Archive.Api.Options;
using Archive.Api.Storage;
using Archive.Core;
using Archive.Core.Options;
using Archive.Core.Storage;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

if (args is ["--healthcheck"])
{
    Environment.ExitCode = await CheckHealthAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<FormOptions>().Configure<ArchiveOptions>((form, archive) =>
    // Allow multipart framing before the controller applies the file limit and returns HTTP 413.
    form.MultipartBodyLengthLimit = checked(archive.MaxUploadBytes + 1_000_000L));
builder.Services.AddOptions<ArchiveApiOptions>().Bind(builder.Configuration)
    .PostConfigure(options => options.PublicBaseUrl = string.IsNullOrWhiteSpace(options.PublicBaseUrl)
        ? null : options.PublicBaseUrl.Trim().TrimEnd('/'))
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton(provider => provider.GetRequiredService<IOptions<ArchiveApiOptions>>().Value);

builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer<ArchiveCreationOperationTransformer>();
});
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(null, allowIntegerValues: false)));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ArchiveExceptionHandler>();

builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(null, allowIntegerValues: false)));

builder.Services.AddArchiveCore(builder.Configuration, initializeDatabase: true);
builder.Services.AddArchiveAuthentication(builder.Configuration, builder.Environment.IsDevelopment());

builder.Services.AddSingleton<IArchiveObjectStore, S3ArchiveObjectStore>();

builder.Services.AddCors(options => options.AddPolicy("public", policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.Services.GetRequiredService<IStartupValidator>().Validate();
var maxUploadRequestBytes = checked(app.Services.GetRequiredService<ArchiveOptions>().MaxUploadBytes + 1_000_000L);
if (app.Services.GetRequiredService<DatabaseOptions>().Initialize)
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<DataContext>();
    await database.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseCors("public");
app.UseAuthentication();
app.UseAuthorization();
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
    app.MapScalarApiReference((options, context) => options
        .WithTitle("Archive API")
        .WithTheme(ScalarTheme.None)
        .ShowOperationId()
        .SortTagsAlphabetically()
        .SortOperationsByMethod()
        .DisableDefaultFonts()
        .DisableAgent());
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
