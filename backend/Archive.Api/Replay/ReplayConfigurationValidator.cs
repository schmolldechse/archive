using Archive.Api.Authentication;
using Archive.Api.Options;

namespace Archive.Api.Replay;

/// <summary>Validates the dedicated replay origin against trusted application and cookie origins.</summary>
internal static class ReplayConfigurationValidator
{
    public static void Validate(ReplayOptions replay, ArchiveApiOptions api,
        ArchiveAuthenticationOptions authentication, IHostEnvironment environment)
    {
        if (!ReplayOptions.TryReadOrigin(replay.PublicOrigin, out var replayOrigin))
            throw new InvalidOperationException("Replay:PublicOrigin is invalid.");
        if (!environment.IsDevelopment() && replayOrigin!.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Replay:PublicOrigin must use HTTPS outside Development.");

        var comparedOrigins = new[]
        {
            authentication.PublicApiOrigin,
            authentication.FrontendOrigin,
            api.PublicBaseUrl
        };
        if (comparedOrigins.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => new Uri(value!, UriKind.Absolute))
            .Any(uri => string.Equals(uri.GetLeftPart(UriPartial.Authority), replay.PublicOrigin,
                StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Replay:PublicOrigin must be distinct from API and frontend origins.");

        var cookieDomain = authentication.SessionCookieDomain?.Trim().TrimStart('.');
        if (!string.IsNullOrWhiteSpace(cookieDomain) &&
            (string.Equals(replayOrigin!.Host, cookieDomain, StringComparison.OrdinalIgnoreCase) ||
             replayOrigin.Host.EndsWith('.' + cookieDomain, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                "Replay:PublicOrigin must be outside Authentication:SessionCookieDomain.");
    }
}
