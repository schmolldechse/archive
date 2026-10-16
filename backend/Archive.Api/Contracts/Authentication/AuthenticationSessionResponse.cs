using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Authentication;

[Description("Authentication state and current user profile.")]
public sealed record AuthenticationSessionResponse
{
    [JsonPropertyName("isAuthenticated")]
    [Description("Whether the request has an authenticated user session.")]
    public required bool IsAuthenticated { get; init; }

    [JsonPropertyName("user")]
    [Description("Current user profile. Null for an anonymous session.")]
    public required UserProfileResponse? User { get; init; }
}
