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

    public string? SessionCookieDomain { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsOrigin(PublicApiOrigin))
            yield return new ValidationResult("PublicApiOrigin must be an absolute HTTP or HTTPS origin.", [nameof(PublicApiOrigin)]);
        if (!IsOrigin(FrontendOrigin))
            yield return new ValidationResult("FrontendOrigin must be an absolute HTTP or HTTPS origin.", [nameof(FrontendOrigin)]);
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

    private static bool IsOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" &&
        uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.Fragment.Length == 0 && uri.UserInfo.Length == 0;
}
