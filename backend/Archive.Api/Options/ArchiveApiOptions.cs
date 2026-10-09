using System.ComponentModel.DataAnnotations;

namespace Archive.Api.Options;

public sealed class ArchiveApiOptions : IValidatableObject
{
    public string? PublicBaseUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PublicBaseUrl is not null &&
            (!Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
             uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0))
            yield return new ValidationResult("PublicBaseUrl must be an absolute HTTP or HTTPS URL without credentials, query or fragment.", [nameof(PublicBaseUrl)]);
    }
}
