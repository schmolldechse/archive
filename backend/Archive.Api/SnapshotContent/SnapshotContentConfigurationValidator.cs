using Archive.Api.Authentication;
using Archive.Api.Options;

namespace Archive.Api.SnapshotContent;

/// <summary>Validates the public snapshot origin against trusted application and cookie origins.</summary>
internal static class SnapshotContentConfigurationValidator
{
    public static void Validate(SnapshotContentOptions options, ArchiveApiOptions api,
        ArchiveAuthenticationOptions authentication, IHostEnvironment environment)
    {
        if (!SnapshotContentOptions.TryReadOrigin(options.PublicOrigin, out var snapshotOrigin))
            throw new InvalidOperationException("SnapshotContent:PublicOrigin is invalid.");
        if (!environment.IsDevelopment() && snapshotOrigin!.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("SnapshotContent:PublicOrigin must use HTTPS outside Development.");

        var comparedOrigins = new[]
        {
            authentication.PublicApiOrigin,
            authentication.FrontendOrigin,
            api.PublicBaseUrl
        };
        if (comparedOrigins.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => new Uri(value!, UriKind.Absolute))
            .Any(uri => string.Equals(uri.GetLeftPart(UriPartial.Authority), options.PublicOrigin,
                StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("SnapshotContent:PublicOrigin must be distinct from API and frontend origins.");

        var cookieDomain = authentication.SessionCookieDomain?.Trim().TrimStart('.');
        if (!string.IsNullOrWhiteSpace(cookieDomain) &&
            (string.Equals(snapshotOrigin!.Host, cookieDomain, StringComparison.OrdinalIgnoreCase) ||
             snapshotOrigin.Host.EndsWith('.' + cookieDomain, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                "SnapshotContent:PublicOrigin must be outside Authentication:SessionCookieDomain.");
    }
}
