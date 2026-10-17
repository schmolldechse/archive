namespace Archive.Api.Authorization;

/// <summary>
/// Named access policies that supplement the individual permission policies.
/// </summary>
public static class ArchiveAuthorizationPolicies
{
    /// <summary>Requires an existing user who has completed registration.</summary>
    public const string RegisteredUser = "Archive.RegisteredUser";

    /// <summary>Requires a registered user whose current database role is Admin.</summary>
    public const string Admin = "Archive.Admin";

    /// <summary>Requires either role-assignment or permission-assignment access.</summary>
    public const string UserAccessRead = "Archive.UserAccessRead";
}
