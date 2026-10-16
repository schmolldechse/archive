using Archive.Api.Authentication;
using Archive.Api.Contracts.Health;
using Archive.Api.Mapping;
using Archive.Api.ExceptionHandling;
using Archive.Api.OpenApi;
using Archive.Api.Options;
using Archive.Api.Serialization;
using Archive.Core;
using Archive.Core.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

if (args is ["--healthcheck"])
{
    Environment.ExitCode = await CheckHealthAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SnapshotUploadLimits>();
builder.Services.AddOptions<FormOptions>().Configure<SnapshotUploadLimits>((form, limits) =>
    // Allow multipart framing before the controller applies the file limit and returns HTTP 413.
    form.MultipartBodyLengthLimit = limits.MaxRequestBodyBytes);
builder.Services.AddOptions<ArchiveApiOptions>().Bind(builder.Configuration)
    .PostConfigure(options => options.PublicBaseUrl = string.IsNullOrWhiteSpace(options.PublicBaseUrl)
        ? null : options.PublicBaseUrl.Trim().TrimEnd('/'))
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton(provider => provider.GetRequiredService<IOptions<ArchiveApiOptions>>().Value);

builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer<SnapshotCreationOperationTransformer>();
    options.AddOperationTransformer<SnapshotEventsOperationTransformer>();
    options.AddSchemaTransformer<SnapshotSchemaTransformer>();
});
builder.Services
    .AddControllers(options => options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .AddJsonOptions(options => ArchiveJson.Configure(options.JsonSerializerOptions));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ArchiveExceptionHandler>();

builder.Services.ConfigureHttpJsonOptions(options => ArchiveJson.Configure(options.SerializerOptions));

builder.Services.AddArchiveCore(builder.Configuration, initializeDatabase: true);
builder.Services.AddSingleton<ArchiveCreatorMapper>();
builder.Services.AddHostedService<Archive.Api.Processing.SnapshotProcessingCleanupService>();
builder.Services.AddSingleton<SnapshotMapper>();
builder.Services.AddArchiveAuthentication(builder.Configuration, builder.Environment.IsDevelopment());

builder.Services.AddCors(options => options.AddPolicy("public", policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.Services.GetRequiredService<IStartupValidator>().Validate();
var uploadLimits = app.Services.GetRequiredService<SnapshotUploadLimits>();
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
        string.Equals(context.Request.Path.Value, "/api/snapshots", StringComparison.OrdinalIgnoreCase) &&
        context.Request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true)
    {
        var bodyLimit = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodyLimit is { IsReadOnly: false })
            bodyLimit.MaxRequestBodySize = uploadLimits.MaxRequestBodyBytes;
    }

    await next(context);
});
app.MapGet("/healthz", () => Results.Ok(new HealthResponse { Status = "ok" }))
    .Produces<HealthResponse>(StatusCodes.Status200OK, "application/json")
    .WithSummary("Returns service health status.");
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

public partial class Program;
