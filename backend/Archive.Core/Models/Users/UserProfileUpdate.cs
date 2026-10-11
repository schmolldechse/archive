using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Text;

namespace Archive.Core.Models.Users;

public sealed record UserProfileUpdate : IValidatableObject
{
    [Required]
    [MinLength(6)] 
    [MaxLength(64)]
    public string DisplayName { get; }

    [Required] 
    [MinLength(4)] 
    [MaxLength(32)]
    public string Handle { get; }

    public UserProfileUpdate(string displayName, string handle)
    {
        DisplayName = NormalizeDisplayName(displayName);
        Handle = NormalizeHandle(handle);
    }

    public static string NormalizeDisplayName(string? value)
    {
        try { return (value ?? "").Trim().Normalize(NormalizationForm.FormC); }
        catch (ArgumentException) { return ""; }
    }
    public static string NormalizeHandle(string? value) => (value ?? "").Trim().ToLowerInvariant();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DisplayName.Any(char.IsControl))
            yield return new("The display name cannot contain control characters.", ["displayName"]);
        if (!Regex.IsMatch(Handle, "^[a-z0-9][a-z0-9_-]{2,30}[a-z0-9]$", RegexOptions.CultureInvariant))
            yield return new("Use letters or digits, with hyphens or underscores between them.", ["handle"]);
    }
}
