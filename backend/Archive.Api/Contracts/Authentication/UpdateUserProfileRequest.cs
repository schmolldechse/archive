using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Api.Validation;
using Archive.Core.Models;
using Archive.Core.Models.Users;

namespace Archive.Api.Contracts.Authentication;

[Description("Display name and handle to save for the authenticated user.")]
public sealed class UpdateUserProfileRequest : IValidatableObject
{
    [Required]
    [StringLength(UserProfileValidation.MaxHandleLength, MinimumLength = UserProfileValidation.MinHandleLength)]
    [JsonPropertyName("handle")]
    [Description("Unique handle after trimming. Letters and digits with internal underscores or hyphens; stored in lowercase.")]
    public required string Handle
    {
        get;
        init => field = UserProfileValidation.NormalizeHandle(value);
    }

    [Required]
    [StringLength(UserProfileValidation.MaxDisplayNameLength, MinimumLength = UserProfileValidation.MinDisplayNameLength)]
    [JsonPropertyName("displayName")]
    [Description("Display name after trimming and Unicode NFC normalization. Control characters are not permitted.")]
    public required string DisplayName
    {
        get;
        init => field = UserProfileValidation.NormalizeDisplayName(value);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        ModelValidation.GetErrors(new UserProfileUpdate(DisplayName, Handle)).ToValidationResults();
}
