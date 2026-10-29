using System.ComponentModel.DataAnnotations;

namespace Archive.Api.Options;

/// <summary>Configures the isolated public origin and private capability lifetime for archived content.</summary>
public sealed class SnapshotContentOptions : IValidatableObject
{
    public const string SectionName = "SnapshotContent";

    [Required]
    public string PublicOrigin { get; set; } = "http://127.0.0.1:5200";

    [Range(1, 1440)]
    public int PrivateAccessTokenLifetimeMinutes { get; init; } = 60;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TryReadOrigin(PublicOrigin, out var origin))
        {
            yield return new ValidationResult(
                "SnapshotContent:PublicOrigin must be an absolute HTTP or HTTPS origin without credentials, path, query or fragment.",
                [nameof(PublicOrigin)]);
            yield break;
        }
        PublicOrigin = origin!.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    internal static bool TryReadOrigin(string? value, out Uri? origin)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out origin) && origin.Scheme is "http" or "https" &&
            origin.AbsolutePath == "/" && origin.UserInfo.Length == 0 && origin.Query.Length == 0 &&
            origin.Fragment.Length == 0)
            return true;

        origin = null;
        return false;
    }
}
