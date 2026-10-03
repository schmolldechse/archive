using System.ComponentModel.DataAnnotations;

namespace Archive.Api.Authentication;

public sealed class ArchiveAuthenticationOptions : IValidatableObject
{
    public const string SectionName = "Authentication";

    [Required, Url]
    public string PublicApiOrigin { get; init; } = "http://localhost:5200";

    [Required, Url]
    public string FrontendOrigin { get; init; } = "http://localhost:5173";

    public string? DataProtectionKeyPath { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsOrigin(PublicApiOrigin))
            yield return new ValidationResult("PublicApiOrigin must be an absolute HTTP or HTTPS origin.", [nameof(PublicApiOrigin)]);
        if (!IsOrigin(FrontendOrigin))
            yield return new ValidationResult("FrontendOrigin must be an absolute HTTP or HTTPS origin.", [nameof(FrontendOrigin)]);
    }

    public string FrontendUrl(string? path) => FrontendOrigin.TrimEnd('/') + LocalReturnUrl(path);

    public static string LocalReturnUrl(string? path) =>
        !string.IsNullOrWhiteSpace(path) && path.Length <= 2048 && path.StartsWith('/') &&
        !path.StartsWith("//", StringComparison.Ordinal) && !path.Contains('\\') && !path.Any(char.IsControl)
            ? path : "/account";

    private static bool IsOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" &&
        uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.Fragment.Length == 0 && uri.UserInfo.Length == 0;
}
