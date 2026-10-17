using Archive.Api.Authentication.Catalog;
using Archive.Api.Authentication.Provider;
using Archive.Api.Authorization;
using Archive.Api.Mapping;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Archive.Api.Authentication;

public static class ArchiveAuthenticationExtension
{
    public const string SessionScheme = "Archive.Session";
    public const string CorsPolicy = "Archive.Frontend";

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
            .ValidateOnStart();

        services.AddSingleton(provider => provider.GetRequiredService<IOptions<ArchiveAuthenticationOptions>>().Value);

        services.AddSingleton<AuthenticationMapper>();

        services.AddSingleton<IAuthenticationProviderCatalog, AuthenticationProviderCatalog>();

        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<ArchiveAuthenticationOptions>((cors, options) =>
            cors.AddPolicy(CorsPolicy, policy => policy
                .WithOrigins(options.FrontendOrigin.TrimEnd('/'))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));

        services.AddArchiveAuthorization();

        var dataProtection = services.AddDataProtection().SetApplicationName("Archive.Api");
        if (!isDevelopment)
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "key-ring")));

        var authentication = services.AddAuthentication(SessionScheme).AddCookie(SessionScheme);
        services.AddOptions<CookieAuthenticationOptions>(SessionScheme)
            .Configure<ArchiveAuthenticationOptions>((cookie, options) =>
        {
            cookie.Cookie.Name = isDevelopment ? "archive.session" : "__Secure-archive.session";
            cookie.Cookie.Domain = options.SessionCookieDomain;
            cookie.Cookie.Path = "/";
            cookie.Cookie.HttpOnly = true;
            cookie.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            cookie.Cookie.SameSite = SameSiteMode.Lax;
            cookie.ExpireTimeSpan = TimeSpan.FromHours(12);
            cookie.SlidingExpiration = false;
        });

        IAuthenticationProvider google = new GoogleProvider();
        services.AddSingleton(google);
        google.AddAuthentication(authentication, isDevelopment);
        return services;
    }
}
