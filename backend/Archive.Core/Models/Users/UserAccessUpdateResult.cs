using Archive.Core.Entities.Authentication;

namespace Archive.Core.Models.Users;

/// <summary>Atomic access update outcomes, mapped to HTTP at the API boundary.</summary>
public abstract record UserAccessUpdateResult
{
    private UserAccessUpdateResult() { }
    public sealed record Updated(ArchiveUser User) : UserAccessUpdateResult;
    public sealed record NotFound : UserAccessUpdateResult;
    public sealed record Forbidden : UserAccessUpdateResult;
    public sealed record Stale : UserAccessUpdateResult;
    public sealed record SelfChange : UserAccessUpdateResult;
    public sealed record LastAdministrator : UserAccessUpdateResult;
}
