using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Authentication;

[Description("Profile information for the authenticated user.")]
public sealed class AuthenticatedArchiveUser
{
    [MaxLength(32)]
    [JsonPropertyName("handle")]
    [Description("Unique lowercase user handle, or an empty string before profile setup. New and updated handles must contain 6 to 32 characters.")]
    public required string Handle { get; set; }

    [MaxLength(64)]
    [JsonPropertyName("displayName")]
    [Description("User display name of 1 to 64 characters.")]
    public required string DisplayName { get; set; }

    [MaxLength(256)]
    [JsonPropertyName("profileImageUrl")]
    [Description("Absolute URL of the stored provider profile image, or null when no image is available.")]
    public required string? ProfileImageUrl { get; set; }
}
