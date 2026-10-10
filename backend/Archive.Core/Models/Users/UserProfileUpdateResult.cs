using Archive.Core.Entities.Authentication;

namespace Archive.Core.Models.Users;

public abstract record UserProfileUpdateResult
{
    private UserProfileUpdateResult() { }
    public sealed record Updated(ArchiveUser User) : UserProfileUpdateResult;
    public sealed record NotFound : UserProfileUpdateResult;
    public sealed record Invalid(IReadOnlyList<FieldValidationError> Errors) : UserProfileUpdateResult;
    public sealed record HandleUnavailable : UserProfileUpdateResult;
}
