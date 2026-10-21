using System.ComponentModel.DataAnnotations;

namespace Archive.Api.Authentication;

public sealed class ArchiveAuthenticationOptions : IValidatableObject
{
    public const string SectionName = "Authentication";

    [Url]
    [Required]
    public string PublicApiOrigin { get; init; } = "http://localhost:5200";

    [Url]
    [Required]
    public string FrontendOrigin { get; init; } = "http://localhost:5173";

    /// <summary>Shared parent domain for the frontend and API session cookie when their hosts differ.</summary>
    public string? SessionCookieDomain { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasApiOrigin = TryReadOrigin(PublicApiOrigin, out var apiOrigin);
        var hasFrontendOrigin = TryReadOrigin(FrontendOrigin, out var frontendOrigin);
        if (!hasApiOrigin)
            yield return new ValidationResult("PublicApiOrigin must be an absolute HTTP or HTTPS origin.", [nameof(PublicApiOrigin)]);
        if (!hasFrontendOrigin)
            yield return new ValidationResult("FrontendOrigin must be an absolute HTTP or HTTPS origin.", [nameof(FrontendOrigin)]);
        if (!hasApiOrigin || !hasFrontendOrigin)
            yield break;

        if (!string.Equals(apiOrigin!.Scheme, frontendOrigin!.Scheme, StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult(
                "PublicApiOrigin and FrontendOrigin must use the same scheme so session cookies remain same-site.",
                [nameof(PublicApiOrigin), nameof(FrontendOrigin)]);

        if (string.IsNullOrWhiteSpace(SessionCookieDomain))
        {
            if (!string.Equals(apiOrigin.Host, frontendOrigin.Host, StringComparison.OrdinalIgnoreCase))
                yield return new ValidationResult(
                    "SessionCookieDomain is required when PublicApiOrigin and FrontendOrigin use different hosts.",
                    [nameof(SessionCookieDomain), nameof(PublicApiOrigin), nameof(FrontendOrigin)]);
            yield break;
        }

        var cookieDomain = SessionCookieDomain.Trim().TrimStart('.');
        if (cookieDomain.EndsWith('.') || !cookieDomain.Contains('.') || Uri.CheckHostName(cookieDomain) != UriHostNameType.Dns)
        {
            yield return new ValidationResult("SessionCookieDomain must be a valid shared DNS domain.", [nameof(SessionCookieDomain)]);
            yield break;
        }

        if (!DomainMatches(apiOrigin.Host, cookieDomain) || !DomainMatches(frontendOrigin.Host, cookieDomain))
            yield return new ValidationResult(
                "SessionCookieDomain must include both PublicApiOrigin and FrontendOrigin.",
                [nameof(SessionCookieDomain), nameof(PublicApiOrigin), nameof(FrontendOrigin)]);
    }

    public string FrontendUrl(string? path) => FrontendOrigin.TrimEnd('/') + LocalReturnUrl(path);

    /// <summary>Returns a validated local path and query without fragment-based navigation.</summary>
    public static string LocalReturnUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 2048 ||
            !path.StartsWith('/') || path.StartsWith("//", StringComparison.Ordinal) ||
            path.Contains('\\') || path.Any(char.IsControl)) return "/";

        if (!Uri.TryCreate(new Uri("https://archive.invalid"), path, out var parsed) || parsed.Host != "archive.invalid") return "/";
        var decodedPath = Uri.UnescapeDataString(parsed.AbsolutePath);
        if (decodedPath.Contains('\\') || decodedPath.Contains('%') || decodedPath.Any(char.IsControl) ||
            parsed.AbsolutePath.Contains("%2f", StringComparison.OrdinalIgnoreCase)) return "/";
        var route = decodedPath.Split('/')[1].ToLowerInvariant();
        if (route is "login" or "api" || route == "account" &&
            decodedPath != "/account/submissions" && !decodedPath.StartsWith("/account/submissions/", StringComparison.Ordinal)) return "/";
        return parsed.PathAndQuery;
    }

    /// <summary>Parses an HTTP origin without accepting paths, query strings, fragments or credentials.</summary>
    private static bool TryReadOrigin(string origin, out Uri? uri)
    {
        if (Uri.TryCreate(origin, UriKind.Absolute, out uri) && uri.Scheme is "http" or "https" &&
            uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.Fragment.Length == 0 && uri.UserInfo.Length == 0)
            return true;

        uri = null;
        return false;
    }

    /// <summary>Checks whether a host is the configured cookie domain or one of its subdomains.</summary>
    private static bool DomainMatches(string host, string domain) =>
        string.Equals(host, domain, StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith('.' + domain, StringComparison.OrdinalIgnoreCase);
}
