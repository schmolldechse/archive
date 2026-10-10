using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Archive.Core.Models.Users;

public sealed record UserProfileUpdate(
    [property: Required, MinLength(6), MaxLength(64)] string DisplayName,
    [property: Required, MinLength(6), MaxLength(32)] string Handle) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DisplayName is not null && DisplayName.Trim().Any(char.IsControl))
            yield return new("The display name cannot contain control characters.", ["displayName"]);
        if (Handle is not null && !Regex.IsMatch(Handle.Trim().ToLowerInvariant(),
            "^[a-z0-9][a-z0-9_-]*[a-z0-9]$", RegexOptions.CultureInvariant))
            yield return new("Use letters or digits, with hyphens or underscores between them.", ["handle"]);
    }
}
