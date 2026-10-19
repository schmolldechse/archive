using Microsoft.AspNetCore.Authorization;

namespace Archive.Api.Authorization.Requirements;

/// <summary>
/// Requires a local user with completed registration and a supported role.
/// </summary>
internal sealed class RegisteredUserRequirement : IAuthorizationRequirement;
