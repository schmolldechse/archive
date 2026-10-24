using System.ComponentModel.DataAnnotations;

namespace Archive.Api.Options;

/// <summary>Required public origin and tested browser boundary for archived JavaScript replay.</summary>
public sealed class ReplayOptions : IValidatableObject
{
    public const string SectionName = "Replay";

    [Required]
    public string PublicOrigin { get; set; } = "http://127.0.0.1:5200";

    [Range(1, int.MaxValue)]
    public int MinimumChromiumMajorVersion { get; init; } = 152;

    [MinLength(1)]
    public string[] AllowedBrands { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TryReadOrigin(PublicOrigin, out var origin))
        {
            yield return new ValidationResult(
                "Replay:PublicOrigin must be an absolute HTTP or HTTPS origin without credentials, path, query or fragment.",
                [nameof(PublicOrigin)]);
            yield break;
        }

        PublicOrigin = origin!.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        if (AllowedBrands.Any(string.IsNullOrWhiteSpace) ||
            AllowedBrands.Distinct(StringComparer.Ordinal).Count() != AllowedBrands.Length)
            yield return new ValidationResult("Replay:AllowedBrands must contain unique non-empty brand names.",
                [nameof(AllowedBrands)]);
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
