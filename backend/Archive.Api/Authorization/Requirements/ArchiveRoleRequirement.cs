using Archive.Core.Entities.Authentication.Permissible;
using Microsoft.AspNetCore.Authorization;

namespace Archive.Api.Authorization.Requirements;

/// <summary>
/// Checks the current database role rather than a role saved in a session cookie.
/// </summary>
/// <param name="Role">Role required by the policy.</param>
internal sealed record ArchiveRoleRequirement(ArchiveRole Role) : IAuthorizationRequirement;
