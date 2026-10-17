namespace Archive.Core.Authorization;

/// <summary>
/// Stable permission keys shared by authorization checks and access management.
/// </summary>
public static class ArchivePermissions
{
    /// <summary>Allows reading, approving, and rejecting submitted snapshots.</summary>
    public const string ReviewSnapshots = "snapshots.review";

    /// <summary>Allows publishing the user's own submissions without manual approval.</summary>
    public const string PublishSnapshotsWithoutApproval = "snapshots.publish_without_approval";

    /// <summary>Allows assigning the User or Admin role to another user.</summary>
    public const string AssignUserRoles = "users.assign_roles";

    /// <summary>Allows setting or removing another user's permission overrides.</summary>
    public const string AssignUserPermissions = "users.assign_permissions";
}
