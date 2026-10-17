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
            ArchivePermissions.ReviewSnapshots,
            "Review snapshots",
            "View submitted snapshots, their previews, and review history. Approve submissions for public access or reject them with an optional reason.",
            [ArchiveRole.Admin]),
        new(
            ArchivePermissions.PublishSnapshotsWithoutApproval,
            "Publish without review",
            "Publish your own snapshot submissions once capture has completed, without waiting for a reviewer to approve them.",
            [ArchiveRole.Admin]),
        new(
            ArchivePermissions.AssignUserRoles,
            "Manage user roles",
            "View user accounts and change another user's role between User and Admin. The assigned role determines which permissions the user receives by default.",
            [ArchiveRole.Admin]),
        new(
            ArchivePermissions.AssignUserPermissions,
            "Manage permission overrides",
            "View a user's access settings and manage individual permission overrides. Allow grants a permission, Deny blocks it, and removing an override restores the role default.",
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
