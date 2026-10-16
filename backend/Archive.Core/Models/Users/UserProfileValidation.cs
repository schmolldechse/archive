using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace Archive.Core.Models.Users;

public static class UserProfileValidation
{
    public const int MinDisplayNameLength = 6;
    public const int MaxDisplayNameLength = 64;
    public const int MinHandleLength = 4;
    public const int MaxHandleLength = 32;

    public static string NormalizeDisplayName(string? value)
    {
        try { return (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormC); }
        catch (ArgumentException) { return string.Empty; }
    }

    public static string NormalizeHandle(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    public static IEnumerable<ValidationResult> Validate(UserProfileUpdate profile)
    {
        if (profile.DisplayName.Any(char.IsControl))
            yield return new("The display name cannot contain control characters.", [nameof(profile.DisplayName)]);
        if (!Regex.IsMatch(profile.Handle, "^[a-z0-9][a-z0-9_-]*[a-z0-9]$", RegexOptions.CultureInvariant))
            yield return new("Use letters or digits, with hyphens or underscores between them.", [nameof(profile.Handle)]);
    }
}
