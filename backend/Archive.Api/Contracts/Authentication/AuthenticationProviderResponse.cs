using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Models.Authentication;

namespace Archive.Api.Contracts.Authentication;

[Description("An available authentication provider.")]
public sealed record AuthenticationProviderResponse
{
    [JsonPropertyName("key")]
    [Description("Provider key used to start authentication.")]
    [StringLength(AuthenticationProviderValidation.MaxKeyLength)]
    public required string Key { get; init; }

    [JsonPropertyName("displayName")]
    [Description("Provider display name.")]
    [StringLength(AuthenticationProviderValidation.MaxDisplayNameLength)]
    public required string DisplayName { get; init; }
}
