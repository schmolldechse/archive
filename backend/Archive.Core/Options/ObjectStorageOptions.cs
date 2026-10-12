using System.ComponentModel.DataAnnotations;

namespace Archive.Core.Options;

public sealed class ObjectStorageOptions : IValidatableObject
{
    public const string SectionName = "ObjectStorage";

    [Required, Url]
    public string ServiceUrl { get; set; } = "http://localhost:9000";

    [Required]
    public string Bucket { get; set; } = "snapshots";

    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(ServiceUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            yield return new ValidationResult("ServiceUrl must be an absolute HTTP or HTTPS URL without credentials, query or fragment.", [nameof(ServiceUrl)]);
        if (string.IsNullOrWhiteSpace(AccessKey) != string.IsNullOrWhiteSpace(SecretKey))
            yield return new ValidationResult("AccessKey and SecretKey must either both be configured or both be omitted.", [nameof(AccessKey), nameof(SecretKey)]);
    }
}
