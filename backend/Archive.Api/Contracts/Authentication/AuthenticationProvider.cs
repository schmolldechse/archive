using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Authentication;

[Description("An external authentication provider.")]
public sealed class AuthenticationProvider
{
    [MaxLength(32)]
    [JsonPropertyName("key")]
    [Description("Provider identifier used to start authentication.")]
    public required string Key { get; set; }

    [MaxLength(32)]
    [JsonPropertyName("displayName")]
    [Description("Provider display name.")]
    public required string DisplayName { get; set; }
}
