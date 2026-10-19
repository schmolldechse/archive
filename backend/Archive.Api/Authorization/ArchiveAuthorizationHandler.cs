using Archive.Api.Authentication;
using Archive.Api.Authorization.Requirements;
using Archive.Api.Exceptions;
using Archive.Core.Authorization;
using Archive.Core.Repositories.UserRepository;
using Microsoft.AspNetCore.Authorization;

namespace Archive.Api.Authorization;

/// <summary>
/// Evaluates archive requirements against the current database user and Core permission rules.
/// </summary>
/// <param name="userRepository">Reads the user and their personal permission overrides.</param>
internal sealed class ArchiveAuthorizationHandler(IUserRepository userRepository) : IAuthorizationHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var requirements = context.Requirements
            .Where(requirement => requirement is
                RegisteredUserRequirement or ArchiveRoleRequirement or PermissionRequirement)
            .ToArray();

        if (requirements.Length == 0)
            return;

        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return;
        }

        var cancellationToken = context.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;
        var user = await userRepository.GetByIdAsync(context.User.ReadUserId(), cancellationToken)
            ?? throw new InvalidSessionException();

        if (!user.IsRegistrationComplete || !Enum.IsDefined(user.Role))
        {
            context.Fail();
            return;
        }

        // Read once for all requirements in this authorization decision.
        foreach (var requirement in requirements)
        {
            var allowed = requirement switch
            {
                RegisteredUserRequirement => true,
                ArchiveRoleRequirement role => user.Role == role.Role,
                PermissionRequirement permission => !permission.AnyOf.IsDefaultOrEmpty &&
                    permission.AnyOf.Any(key => PermissionEvaluator.IsAllowed(user, key)),
                _ => false
            };

            if (allowed)
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }
}
