using System.Security.Claims;
using Archive.Core.Models.Authentication;
using Archive.Core.Repositories.AuthenticationRepository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;

namespace Archive.Api.Authentication.Provider;

public sealed class GoogleProvider : IAuthenticationProvider
{
    public string Id => "google";
    public string DisplayName => "Google";
    public string Scheme => GoogleDefaults.AuthenticationScheme;

    public void AddAuthentication(AuthenticationBuilder authentication, bool isDevelopment)
    {
        authentication.Services.AddOptions<GoogleProviderOptions>()
            .BindConfiguration(GoogleProviderOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        authentication.Services.AddHttpClient("GoogleProfileImage", client => client.Timeout = TimeSpan.FromSeconds(5))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        authentication.AddGoogle();
        authentication.Services.AddOptions<GoogleOptions>(Scheme)
            .Configure<ArchiveAuthenticationOptions, IOptions<GoogleProviderOptions>>((google, options, provider) =>
        {
            google.ClientId = provider.Value.ClientId;
            google.ClientSecret = provider.Value.ClientSecret;
            google.SignInScheme = ArchiveAuthenticationExtension.SessionScheme;
            google.CallbackPath = "/api/auth/callback/google";
            google.UsePkce = true;
            google.SaveTokens = false;
            google.Scope.Clear();
            google.Scope.Add("openid");
            google.Scope.Add("profile");
            google.CorrelationCookie.SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None;
            google.CorrelationCookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            google.Events.OnCreatingTicket = context => SignInAsync(context, options);
            google.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect(options.FrontendUrl("/login?error=provider_failed"));
                return Task.CompletedTask;
            };
        });
    }

    private async Task SignInAsync(OAuthCreatingTicketContext context, ArchiveAuthenticationOptions options)
    {
        var subject = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject)) throw new InvalidOperationException("Google did not return a subject.");

        var displayName = context.Principal?.FindFirstValue(ClaimTypes.Name) ?? "User";
        var pictureUrl = context.User.TryGetProperty("picture", out var picture) && picture.ValueKind == System.Text.Json.JsonValueKind.String
            ? picture.GetString() : null;
        var image = await DownloadPictureAsync(context.HttpContext, pictureUrl);
        var repository = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationRepository>();
        var user = await repository.GetOrCreateUserAsync(new ExternalIdentityDescriptor(
            Id, DisplayName, subject, context.Principal?.FindFirstValue(ClaimTypes.Email), displayName, image),
            context.HttpContext.RequestAborted);

        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString("D")),
            new Claim(ClaimTypes.Name, user.DisplayName)
        ], ArchiveAuthenticationExtension.SessionScheme));
        context.Properties.RedirectUri = options.FrontendUrl(
            string.IsNullOrEmpty(user.Handle) ? "/account/setup" : context.Properties.RedirectUri);
    }

    private static async Task<byte[]?> DownloadPictureAsync(HttpContext context, string? pictureUrl)
    {
        if (!Uri.TryCreate(pictureUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !(uri.Host.EndsWith(".googleusercontent.com", StringComparison.OrdinalIgnoreCase) ||
              uri.Host.EndsWith(".ggpht.com", StringComparison.OrdinalIgnoreCase))) return null;

        try
        {
            var client = context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("GoogleProfileImage");
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
            const int maxBytes = 2 * 1024 * 1024;
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > maxBytes) return null;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var image = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(buffer, timeout.Token)) != 0)
            {
                if (image.Length + count > maxBytes) return null;
                image.Write(buffer, 0, count);
            }
            var bytes = image.ToArray();
            return ProfileImageContent.ContentType(bytes) is null ? null : bytes;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException ||
                                          exception is OperationCanceledException && !context.RequestAborted.IsCancellationRequested)
        {
            // A missing provider picture must not prevent sign-in.
            return null;
        }
    }
}
