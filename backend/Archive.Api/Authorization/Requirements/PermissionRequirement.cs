using System.Collections.Immutable;
using Microsoft.AspNetCore.Authorization;

namespace Archive.Api.Authorization.Requirements;

/// <summary>
/// Requires at least one of the supplied permissions to be effectively allowed.
/// </summary>
/// <param name="AnyOf">Exact permission keys; an empty list does not grant access.</param>
internal sealed record PermissionRequirement(ImmutableArray<string> AnyOf) : IAuthorizationRequirement;
