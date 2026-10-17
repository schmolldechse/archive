using System.Collections.Immutable;
using Archive.Core.Entities.Authentication.Permissible;

namespace Archive.Core.Authorization;

/// <summary>
/// Immutable description of a known permission and its role requirements.
/// </summary>
/// <param name="Key">Stable, case-sensitive permission key.</param>
/// <param name="DisplayName">English name shown in access management.</param>
/// <param name="Description">English description of the permitted action.</param>
/// <param name="DefaultRoles">Roles that grant this permission without a personal override.</param>
public sealed record PermissionDefinition(
    string Key,
    string DisplayName,
    string Description,
    ImmutableArray<ArchiveRole> DefaultRoles);
