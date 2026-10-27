using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Archive.Core.Entities.Authentication.Permissible;

namespace Archive.Core.Authorization;

/// <summary>
/// Defines the supported permissions and their defaults in a stable display order.
/// </summary>
public static class PermissionCatalog
{
    /// <summary>All supported permission definitions; callers cannot modify the catalog.</summary>
    public static ImmutableArray<PermissionDefinition> All { get; } =
    [
        new(
            ArchivePermissions.AccessAdminPortal,
            "Access admin portal",
            "Open the admin portal. Access to individual sections and administrative actions requires their respective permissions.",
            [ArchiveRole.Admin]),
        new(
            ArchivePermissions.AccessAdminUsers,
            "Access user administration",
            "Open the Users section and view user profiles, roles, and individual permission settings. Changing access requires the corresponding management permission.",
            [ArchiveRole.Admin]),
        new(
            ArchivePermissions.AccessAdminSnapshots,
            "Access snapshot moderation",
            "View, edit, permanently delete, and moderate submissions, previews, attribution, and snapshot history. Approve or reject snapshots with an optional reason. Requires access to the admin portal.",
            [ArchiveRole.Admin]),
        new(
            ArchivePermissions.PublishSnapshotsWithoutApproval,
            "Publish without review",
            "Publish your own snapshot submissions once capture has completed, without waiting for a reviewer to approve them.",
            [ArchiveRole.Admin]),
        new(
            ArchivePermissions.AssignUserRoles,
            "Manage user roles",
            "Change another user's role between User and Admin. The role determines default permissions; existing personal overrides remain unchanged. Requires access to the admin portal and user administration.",
            [ArchiveRole.Admin]),
        new(
            ArchivePermissions.AssignUserPermissions,
            "Manage permission overrides",
            "Set, replace, or remove another user's individual permission overrides. Allow grants a permission, Deny blocks it, and removing an override restores the role default. Requires access to the admin portal and user administration.",
            [ArchiveRole.Admin])
    ];

    private static readonly FrozenDictionary<string, PermissionDefinition> Definitions =
        All.ToFrozenDictionary(definition => definition.Key, StringComparer.Ordinal);

    /// <summary>
    /// Looks up an exact key without normalizing unknown or differently cased input.
    /// </summary>
    /// <param name="key">Permission key to look up.</param>
    /// <param name="definition">The definition when the key is known; otherwise null.</param>
    /// <returns>Whether the key identifies a supported permission.</returns>
    public static bool TryGet(string? key, [NotNullWhen(true)] out PermissionDefinition? definition)
    {
        definition = null;
        return key is not null && Definitions.TryGetValue(key, out definition);
    }
}
