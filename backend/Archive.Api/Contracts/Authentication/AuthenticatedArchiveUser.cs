using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Authentication;

[Description("Profile information for the authenticated user.")]
public sealed class AuthenticatedArchiveUser
{
    [MaxLength(32)]
    [JsonPropertyName("handle")]
    [Description("Unique lowercase user handle of 4 to 32 characters, or null until the user chooses one.")]
    public required string? Handle { get; set; }

    [MaxLength(64)]
    [JsonPropertyName("displayName")]
    [Description("User display name of 6 to 64 characters, or null until the user chooses one.")]
    public required string? DisplayName { get; set; }

    [MaxLength(256)]
    [JsonPropertyName("profileImageUrl")]
    [Description("Absolute URL of the stored provider profile image, or null when no image is available.")]
    public required string? ProfileImageUrl { get; set; }

    [JsonPropertyName("isRegistrationComplete")]
    [Description("True only after valid display name and handle have been explicitly saved and registration completion has been recorded.")]
    public required bool IsRegistrationComplete { get; set; }
}
