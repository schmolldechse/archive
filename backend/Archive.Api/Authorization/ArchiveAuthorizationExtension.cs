using Archive.Api.Authentication;
using Archive.Api.Authorization.Requirements;
using Archive.Core.Authorization;
using Archive.Core.Entities.Authentication.Permissible;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Archive.Api.Authorization;

/// <summary>
/// Registers the database-backed archive role and permission policies.
/// </summary>
public static class ArchiveAuthorizationExtension
{
    /// <summary>
    /// Adds named archive policies while preserving the default and public endpoint behavior.
    /// </summary>
    /// <param name="services">Application services using the archive session scheme.</param>
    /// <returns>The service collection for further registration.</returns>
    public static IServiceCollection AddArchiveAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, ArchiveAuthorizationHandler>());
        services.AddAuthorization(options =>
        {
            AddRegisteredPolicy(options, ArchiveAuthorizationPolicies.RegisteredUser);
            AddRegisteredPolicy(options, ArchiveAuthorizationPolicies.Admin,
                new ArchiveRoleRequirement(ArchiveRole.Admin));

            foreach (var permission in PermissionCatalog.All)
                AddRegisteredPolicy(options, permission.Key,
                    new PermissionRequirement([permission.Key]));

            AddRegisteredPolicy(options, ArchiveAuthorizationPolicies.UserAccessRead,
                new PermissionRequirement([
                    ArchivePermissions.AssignUserRoles,
                    ArchivePermissions.AssignUserPermissions
                ]));
        });

        return services;
    }

    /// <summary>Applies the common session and registration requirements to a named policy.</summary>
    private static void AddRegisteredPolicy(AuthorizationOptions options, string name, params IAuthorizationRequirement[] requirements) =>
        options.AddPolicy(name, policy => policy
            .AddAuthenticationSchemes(ArchiveAuthenticationExtension.SessionScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(new RegisteredUserRequirement())
            .AddRequirements(requirements));
}
