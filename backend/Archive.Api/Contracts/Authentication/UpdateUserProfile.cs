using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Models.Users;

namespace Archive.Api.Contracts.Authentication;

/// <summary>Display name and handle for the authenticated user.</summary>
/// <example>{"handle":"archive_reader","displayName":"Archive Reader"}</example>
[Description("Replaces the authenticated user's display name and handle.")]
public sealed class UpdateUserProfile : IValidatableObject
{
    [Required]
    [MinLength(4)]
    [MaxLength(32)]
    [JsonPropertyName("handle")]
    [Description("Unique handle of 4 to 32 characters after trimming. Letters, digits, internal underscores and hyphens; starts and ends with a letter or digit. Stored in lowercase.")]
    public required string Handle { get; set; }

    [Required]
    [MinLength(6)]
    [MaxLength(64)]
    [JsonPropertyName("displayName")]
    [Description("Display name of 6 to 64 characters after trimming and Unicode NFC normalization. Control characters are not permitted.")]
    public required string DisplayName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        new UserProfileUpdate(DisplayName, Handle).Validate(validationContext);
}
