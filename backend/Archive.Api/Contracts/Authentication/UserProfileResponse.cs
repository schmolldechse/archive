using System.ComponentModel;
using Archive.Core.Entities.Authentication.Permissible;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Models.Users;

namespace Archive.Api.Contracts.Authentication;

[Description("A user profile without internal identifiers.")]
public sealed class UserProfileResponse
{
    /// <summary>The assigned role; individual overrides can change its default permissions.</summary>
    [JsonPropertyName("role")]
    [Description("Assigned role. Personal permission overrides may extend or restrict its defaults.")]
    public required ArchiveRole Role { get; init; }

    [JsonPropertyName("handle")]
    [Description("Lowercase user handle. Null until a handle is saved.")]
    [StringLength(UserProfileValidation.MaxHandleLength, MinimumLength = UserProfileValidation.MinHandleLength)]
    public required string? Handle { get; init; }

    [JsonPropertyName("displayName")]
    [Description("User display name. Null until a display name is saved.")]
    [StringLength(UserProfileValidation.MaxDisplayNameLength, MinimumLength = UserProfileValidation.MinDisplayNameLength)]
    public required string? DisplayName { get; init; }

    [JsonPropertyName("profileImageUrl")]
    [Description("URL of the stored profile image. Null when no image is stored.")]
    [Url]
    public required string? ProfileImageUrl { get; init; }

    [JsonPropertyName("isRegistrationComplete")]
    [Description("Whether a valid handle and display name have been saved.")]
    public required bool IsRegistrationComplete { get; init; }
}
