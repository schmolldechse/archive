using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts;

[Description("A configured external provider available for voluntary sign-in.")]
public sealed record AuthenticationProviderResponse(
    [property: Description("Stable provider key used to select the sign-in provider."), JsonPropertyName("key")] string Key,
    [property: Description("Human-readable provider name."), JsonPropertyName("displayName")] string DisplayName,
    [property: Description("Absolute API URL accepting the sign-in form."), JsonPropertyName("loginUrl")] string LoginUrl);

[Description("The current optional session. Anonymous visitors have a null user.")]
public sealed record AuthenticationSessionResponse(
    [property: Description("Whether a valid local user session exists."), JsonPropertyName("isAuthenticated")] bool IsAuthenticated,
    [property: Description("Current local user, or null for an anonymous visitor."), JsonPropertyName("user")] UserResponse? User);

[Description("Form fields used to start an external sign-in.")]
public sealed class LoginRequest
{
    [Description("Local frontend path after sign-in; defaults to /account. Users without a chosen handle go to /account/setup first."), JsonPropertyName("returnUrl"), StringLength(2048)]
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "returnUrl")]
    public string? ReturnUrl { get; init; }
}

[Description("The authenticated user's editable profile. External identities and tokens are never exposed.")]
public sealed record UserResponse(
    [property: Description("Stable local user identifier."), JsonPropertyName("id")] Guid Id,
    [property: Description("Display name, between 1 and 64 characters."), JsonPropertyName("displayName")] string DisplayName,
    [property: Description("Unique, editable lowercase handle, between 3 and 32 ASCII characters; empty until profile setup."), JsonPropertyName("handle")] string Handle,
    [property: Description("Absolute URL of the locally stored provider image including its content fingerprint, or null when initials should be shown."), JsonPropertyName("profileImageUrl")] string? ProfileImageUrl);

[Description("Replaces the authenticated user's display name and handle.")]
public sealed class UpdateUserProfileRequest
{
    [Description("Trimmed display name; controls are forbidden."), JsonPropertyName("displayName"), Required, StringLength(64, MinimumLength = 1)]
    public required string DisplayName { get; init; }

    [Description("Unique handle. Letters, digits, internal underscores and hyphens; starts and ends with a letter or digit."), JsonPropertyName("handle"), Required, StringLength(32, MinimumLength = 3)]
    public required string Handle { get; init; }
}
