using System.Security.Cryptography;
using Archive.Api.Authentication.Catalog;
using Archive.Api.Authentication.Provider;
using Archive.Api.Mapping;
using Archive.Core.Entities.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;

namespace Archive.Api.Authentication;

public static class ArchiveAuthenticationExtension
{
    public const string SessionScheme = "Archive.Session";
    public const string CorsPolicy = "Archive.Frontend";
    internal const string CurrentUserKey = "Archive.CurrentUser";

    public static IServiceCollection AddArchiveAuthentication(this IServiceCollection services,
        IConfiguration configuration, bool isDevelopment)
    {
        services.AddOptions<ArchiveAuthenticationOptions>()
            .Bind(configuration.GetRequiredSection(ArchiveAuthenticationOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => isDevelopment ||
                options.PublicApiOrigin?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true &&
                options.FrontendOrigin?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true,
                "Authentication requires HTTPS API and frontend origins outside development.")
            .Validate(options => isDevelopment || !string.IsNullOrWhiteSpace(options.DataProtectionKeyPath),
                "Authentication:DataProtectionKeyPath is required outside development.")
            .ValidateOnStart();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<ArchiveAuthenticationOptions>>().Value);
        services.AddSingleton<AuthenticationMapper>();
        services.AddSingleton<IAuthenticationProviderCatalog, AuthenticationProviderCatalog>();
        services.AddScoped<ArchiveCookieAuthenticationEvents>();
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var path = context.HttpContext.Request.Path;
            if (context.ProblemDetails.Status != StatusCodes.Status415UnsupportedMediaType ||
                !(path.StartsWithSegments("/api/auth") || path.StartsWithSegments("/api/users/me"))) return;

            context.ProblemDetails.Detail = "Use the content type required by this endpoint.";
            context.ProblemDetails.Extensions["code"] = "unsupported_media_type";
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        });
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<ArchiveAuthenticationOptions>((cors, options) =>
            cors.AddPolicy(CorsPolicy, policy => policy
                .WithOrigins(options.FrontendOrigin.TrimEnd('/')).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
        services.AddAuthorization();
        services.AddDataProtection().SetApplicationName("Archive.Api");
        services.AddOptions<KeyManagementOptions>().Configure<ArchiveAuthenticationOptions, ILoggerFactory>((keys, options, logging) =>
        {
            if (!string.IsNullOrWhiteSpace(options.DataProtectionKeyPath))
                keys.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(options.DataProtectionKeyPath), logging);
        });

        var authentication = services.AddAuthentication(SessionScheme).AddCookie(SessionScheme, cookie =>
        {
            cookie.Cookie.Name = isDevelopment ? "archive.session" : "__Host-archive.session";
            cookie.Cookie.Path = "/";
            cookie.Cookie.HttpOnly = true;
            cookie.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            cookie.Cookie.SameSite = SameSiteMode.Lax;
            cookie.ExpireTimeSpan = TimeSpan.FromHours(12);
            cookie.SlidingExpiration = false;
            cookie.EventsType = typeof(ArchiveCookieAuthenticationEvents);
        });

        IAuthenticationProvider google = new GoogleProvider();
        services.AddSingleton(google);
        google.AddAuthentication(authentication, isDevelopment);
        return services;
    }

    public static IApplicationBuilder UseArchiveAuthentication(this IApplicationBuilder app)
    {
        var origin = new Uri(app.ApplicationServices.GetRequiredService<ArchiveAuthenticationOptions>().PublicApiOrigin);
        var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("Archive.Api.Authentication");
        app.Use(async (context, next) =>
        {
            var authenticationRequest = context.Request.Path.StartsWithSegments("/api/auth") ||
                context.Request.Path.StartsWithSegments("/api/users/me");
            if (!authenticationRequest)
            {
                await next(context);
                return;
            }

            try
            {
                // OAuth callback URLs must use the configured public origin, including behind a proxy.
                if (context.Request.Path.StartsWithSegments("/api/auth"))
                {
                    context.Request.Scheme = origin.Scheme;
                    context.Request.Host = HostString.FromUriComponent(origin);
                }
                await next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
            catch (BadHttpRequestException exception) when (!context.Response.HasStarted)
            {
                await Results.Problem(statusCode: exception.StatusCode, detail: "The request body is invalid or too large.",
                    extensions: new Dictionary<string, object?> { ["code"] = "invalid_request", ["traceId"] = context.TraceIdentifier })
                    .ExecuteAsync(context);
            }
            catch (Exception exception) when (!context.Response.HasStarted)
            {
                logger.LogError(exception, "Authentication/profile request failed. Trace {TraceId}", context.TraceIdentifier);
                await Results.Problem(statusCode: 503, detail: "The profile service is temporarily unavailable.",
                    extensions: new Dictionary<string, object?> { ["code"] = "authentication_unavailable", ["traceId"] = context.TraceIdentifier })
                    .ExecuteAsync(context);
            }
        });
        return app.UseAuthentication().UseAuthorization();
    }

    public static ArchiveUser? CurrentUser(HttpContext context) => context.Items[CurrentUserKey] as ArchiveUser;

    public static string? ImageUrl(ArchiveAuthenticationOptions options, ArchiveUser user) =>
        user.ProfileImage is { Length: > 0 } image
            ? $"{options.PublicApiOrigin.TrimEnd('/')}/api/users/me/profile-image?v={Convert.ToHexStringLower(SHA256.HashData(image))}"
            : null;
}
