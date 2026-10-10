using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Archive.Api.Contracts.Authentication;

/// <summary>Display name and handle for the authenticated user.</summary>
/// <example>{"handle":"archive_reader","displayName":"Archive Reader"}</example>
[Description("Replaces the authenticated user's display name and handle.")]
public sealed class UpdateUserProfile : IValidatableObject
{
    [Required]
    [MinLength(6)]
    [MaxLength(32)]
    [JsonPropertyName("handle")]
    [Description("Unique handle of 6 to 32 characters. Letters, digits, internal underscores and hyphens; starts and ends with a letter or digit. Stored in lowercase.")]
    public required string Handle { get; set; }

    [Required]
    [MinLength(6)]
    [MaxLength(64)]
    [JsonPropertyName("displayName")]
    [Description("Display name of 6 to 64 characters after trimming and Unicode NFC normalization. Control characters are not permitted.")]
    public required string DisplayName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DisplayName is not null && DisplayName.Trim().Any(char.IsControl))
            yield return new ValidationResult("The display name cannot contain control characters.", ["displayName"]);

        if (Handle is not null && !Regex.IsMatch(Handle.Trim().ToLowerInvariant(),
            "^[a-z0-9][a-z0-9_-]*[a-z0-9]$", RegexOptions.CultureInvariant))
            yield return new ValidationResult(
                "Use letters or digits, with hyphens or underscores between them.", ["handle"]);
    }
}
