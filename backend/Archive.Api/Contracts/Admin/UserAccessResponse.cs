using System.ComponentModel;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Archive.Core.Entities.Authentication.Permissible;
using Archive.Core.Models.Users;

namespace Archive.Api.Contracts.Admin;

/// <summary>A registered account's saved role and permission settings, addressed by handle.</summary>
[Description("A registered account's saved role and permission settings, addressed by handle.")]
public sealed class UserAccessResponse
{
    [JsonPropertyName("handle")]
    [Description("Current unique lowercase handle of this registered user.")]
    [StringLength(UserProfileValidation.MaxHandleLength, MinimumLength = UserProfileValidation.MinHandleLength)]
    public required string Handle { get; init; }

    [JsonPropertyName("displayName")]
    [Description("Current display name of the registered user.")]
    [StringLength(UserProfileValidation.MaxDisplayNameLength, MinimumLength = UserProfileValidation.MinDisplayNameLength)]
    public required string DisplayName { get; init; }

    [JsonPropertyName("role")]
    [Description("Saved role whose defaults are evaluated before personal overrides.")]
    public required ArchiveRole Role { get; init; }

    [JsonPropertyName("permissions")]
    [Description("Saved evaluation for every supported permission in catalog order.")]
    public required IReadOnlyList<UserPermissionResponse> Permissions { get; init; }
}
