using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Authentication;

[Description("Replaces the authenticated user's display name and handle.")]
public sealed class UpdateUserProfile
{
    [MinLength(6)]
    [MaxLength(32)]
    [JsonPropertyName("handle")]
    [Description("Unique handle. Letters, digits, internal underscores and hyphens; starts and ends with a letter or digit.")]
    public required string Handle { get; set; }

    [MinLength(6)]
    [MaxLength(64)]
    [JsonPropertyName("displayName")]
    [Description("Trimmed display name; controls are forbidden.")]
    public required string DisplayName { get; set; }
}
