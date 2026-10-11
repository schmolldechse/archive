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
    private const string PictureClaimType = "urn:google:picture";
    private const int MaxProfileImageBytes = 2 * 1024 * 1024;

    public string Id => "google";
    public string DisplayName => "Google";
    public string Scheme => GoogleDefaults.AuthenticationScheme;

    public void AddAuthentication(AuthenticationBuilder authentication, bool isDevelopment)
    {
        authentication.Services.AddOptions<GoogleProviderOptions>()
            .BindConfiguration(GoogleProviderOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        authentication.Services.AddHttpClient("GoogleProfileImage", client => client.Timeout = TimeSpan.FromSeconds(3))
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

            google.ClaimActions.MapJsonKey(PictureClaimType, "picture", "url");

            google.CorrelationCookie.SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None;
            google.CorrelationCookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

            google.Events.OnCreatingTicket = context => SignInAsync(context, options);
        });
    }

    private async Task SignInAsync(OAuthCreatingTicketContext context, ArchiveAuthenticationOptions options)
    {
        var subject = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject)) throw new InvalidOperationException("Google did not return a subject.");

        var displayName = context.Principal?.FindFirstValue(ClaimTypes.Name) ?? "User";
        var pictureUrl = context.Principal?.FindFirstValue(PictureClaimType);
        var image = await DownloadPictureAsync(context.HttpContext, pictureUrl);

        var repository = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationRepository>();
        var user = await repository.GetOrCreateUserAsync(
            new ExternalIdentityDescriptor(Id, DisplayName, subject, context.Principal?.FindFirstValue(ClaimTypes.Email), displayName, image),
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
        if (string.IsNullOrWhiteSpace(pictureUrl)) return null;

        var client = context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("GoogleProfileImage");

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        deadline.CancelAfter(TimeSpan.FromSeconds(3));

        using var response = await client.GetAsync(pictureUrl, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxProfileImageBytes) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);

        using var image = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, deadline.Token)) != 0)
        {
            if (image.Length + count > MaxProfileImageBytes) return null;
            image.Write(buffer, 0, count);
        }

        var bytes = image.ToArray();
        return ProfileImageContent.ContentType(bytes) is null ? null : bytes;
    }
}
