using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Authentication;

[Description("Authentication state and current user profile.")]
public sealed class AuthenticationSessionResponse
{
    /// <summary>Individually effective keys. Operations may require several keys together.</summary>
    [JsonPropertyName("permissions")]
    [Description("Effective permission keys in catalog order. Empty for anonymous or incomplete accounts. Operation prerequisites must be checked together.")]
    public required IReadOnlyList<string> Permissions { get; init; }

    [JsonPropertyName("isAuthenticated")]
    [Description("Whether the request has an authenticated user session.")]
    public required bool IsAuthenticated { get; init; }

    [JsonPropertyName("user")]
    [Description("Current user profile. Null for an anonymous session.")]
    public required UserProfileResponse? User { get; init; }
}
