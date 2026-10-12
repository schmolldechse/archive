using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Authentication;

[Description("The current authentication state and associated user profile.")]
public sealed class AuthenticationSession
{
    [JsonPropertyName("isAuthenticated")]
    [Description("Indicates whether the request has a valid user session.")]
    public required bool IsAuthenticated { get; set; }

    [JsonPropertyName("user")]
    [Description("Authenticated user profile, or null for an anonymous request.")]
    public AuthenticatedArchiveUser? User { get; set; }
}
