using System.Collections.Immutable;
using Archive.Core.Entities.Authentication;
using Archive.Core.Entities.Authentication.Permissible;
using Archive.Core.Models.Users;

namespace Archive.Core.Authorization;

/// <summary>
/// Evaluates access from a supplied user projection without HTTP or database dependencies.
/// </summary>
public static class PermissionEvaluator
{
    /// <summary>
    /// Applies registration and role requirements before a personal override or role default.
    /// </summary>
    /// <param name="user">Current database access data, or null when the user does not exist.</param>
    /// <param name="permission">Exact permission key to evaluate.</param>
    /// <returns>True only when a known permission is explicitly allowed.</returns>
    public static bool IsAllowed(ArchiveUser? user, string? permission)
    {
        if (user is not { IsRegistrationComplete: true } ||
            !Enum.IsDefined(user.Role) ||
            !PermissionCatalog.TryGet(permission, out var definition))
            return false;

        // The database key guarantees at most one override per user and permission.
        var permissionOverride = user.PermissionOverrides.SingleOrDefault(
            entry => string.Equals(entry.Permission, definition.Key, StringComparison.Ordinal));

        return permissionOverride is not null
            ? permissionOverride.Effect == ArchivePermissionEffect.Allow
            : definition.DefaultRoles.Contains(user.Role);
    }

    /// <summary>
    /// Returns the effective known permissions in catalog order using the same decision rules.
    /// </summary>
    /// <param name="user">Current database access data, or null when the user does not exist.</param>
    /// <returns>An immutable list of effectively allowed permission keys.</returns>
    public static ImmutableArray<string> GetAllowedPermissions(ArchiveUser? user) =>
        PermissionCatalog.All
            .Where(definition => IsAllowed(user, definition.Key))
            .Select(definition => definition.Key)
            .ToImmutableArray();
}
