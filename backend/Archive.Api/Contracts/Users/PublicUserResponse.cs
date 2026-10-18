using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Entities.Authentication.Permissible;
using Archive.Core.Models.Users;

namespace Archive.Api.Contracts.Users;

/// <summary>Public account information suitable for attribution.</summary>
[Description("Public account information suitable for snapshot and history attribution.")]
public sealed class PublicUserResponse
{
    [JsonPropertyName("handle")]
    [Description("Current lowercase handle of the user.")]
    [StringLength(UserProfileValidation.MaxHandleLength, MinimumLength = UserProfileValidation.MinHandleLength)]
    public required string Handle { get; init; }

    [JsonPropertyName("displayName")]
    [Description("Current display name of the user.")]
    [StringLength(UserProfileValidation.MaxDisplayNameLength, MinimumLength = UserProfileValidation.MinDisplayNameLength)]
    public required string DisplayName { get; init; }

    [JsonPropertyName("profileImageUrl")]
    [Description("Public profile image URL. Null when no image is stored.")]
    [Url]
    public required string? ProfileImageUrl { get; init; }

    [JsonPropertyName("role")]
    [Description("Current account role.")]
    public required ArchiveRole Role { get; init; }
}
