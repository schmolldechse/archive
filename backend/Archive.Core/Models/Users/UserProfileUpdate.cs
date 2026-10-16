using System.ComponentModel.DataAnnotations;

namespace Archive.Core.Models.Users;

public sealed record UserProfileUpdate : IValidatableObject
{
    [Required]
    [StringLength(UserProfileValidation.MaxDisplayNameLength, MinimumLength = UserProfileValidation.MinDisplayNameLength)]
    public string DisplayName { get; }

    [Required]
    [StringLength(UserProfileValidation.MaxHandleLength, MinimumLength = UserProfileValidation.MinHandleLength)]
    public string Handle { get; }

    public UserProfileUpdate(string displayName, string handle)
    {
        DisplayName = UserProfileValidation.NormalizeDisplayName(displayName);
        Handle = UserProfileValidation.NormalizeHandle(handle);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        UserProfileValidation.Validate(this);
}
