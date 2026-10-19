using Archive.Api.Authentication;
using Archive.Api.Contracts.Admin;
using Archive.Api.Contracts.Authentication;
using Archive.Core.Authorization;
using Archive.Core.Entities.Authentication;
using Archive.Core.Entities.Authentication.Permissible;
using Archive.Core.Models.Users;
using Riok.Mapperly.Abstractions;

namespace Archive.Api.Mapping;

/// <summary>Maps administration projections to public contracts through Mapperly.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class UserAccessMapper(ArchiveAuthenticationOptions options)
{
    /// <summary>Maps the page through the shared profile contract.</summary>
    public partial UserListResponse ToResponse(UserListPage page);

    /// <summary>Publishes catalog text without internal role defaults.</summary>
    [MapperIgnoreSource(nameof(PermissionDefinition.DefaultRoles))]
    public partial PermissionDefinitionResponse ToResponse(PermissionDefinition definition);

    /// <summary>Registered-user projections always represent complete profiles.</summary>
    public UserProfileResponse ToResponse(UserListEntry user) => MapProfile(user, true);

    [MapperIgnoreSource(nameof(UserListEntry.HasProfileImage))]
    [MapPropertyFromSource(nameof(UserProfileResponse.ProfileImageUrl), Use = nameof(GetProfileImageUrl))]
    private partial UserProfileResponse MapProfile(UserListEntry user, bool isRegistrationComplete);

    // List images must use each target's public handle, rather than the operator's self-image route.
    private string? GetProfileImageUrl(UserListEntry user) => user.HasProfileImage
        ? $"{options.PublicApiOrigin.TrimEnd('/')}/api/users/{Uri.EscapeDataString(user.Handle)}/profile-image" : null;

    /// <summary>Evaluates permissions in Core and delegates public property mapping to Mapperly.</summary>
    [MapperIgnoreSource(nameof(ArchiveUser.Id))]
    [MapperIgnoreSource(nameof(ArchiveUser.ProfileImage))]
    [MapperIgnoreSource(nameof(ArchiveUser.CreatedAt))]
    [MapperIgnoreSource(nameof(ArchiveUser.UpdatedAt))]
    [MapperIgnoreSource(nameof(ArchiveUser.RegistrationCompletedAt))]
    [MapperIgnoreSource(nameof(ArchiveUser.IsRegistrationComplete))]
    [MapperIgnoreSource(nameof(ArchiveUser.Identities))]
    [MapperIgnoreSource(nameof(ArchiveUser.PermissionOverrides))]
    [MapPropertyFromSource(nameof(UserAccessResponse.Permissions), Use = nameof(GetPermissions))]
    public partial UserAccessResponse ToResponse(ArchiveUser user);

    private IReadOnlyList<UserPermissionResponse> GetPermissions(ArchiveUser user) => PermissionCatalog.All
        .Select(definition => MapPermission(definition, definition.DefaultRoles.Contains(user.Role),
            user.PermissionOverrides.FirstOrDefault(item => item.Permission == definition.Key)?.Effect,
            PermissionEvaluator.IsAllowed(user, definition.Key))).ToArray();

    [MapperIgnoreSource(nameof(PermissionDefinition.DisplayName))]
    [MapperIgnoreSource(nameof(PermissionDefinition.Description))]
    [MapperIgnoreSource(nameof(PermissionDefinition.DefaultRoles))]
    private partial UserPermissionResponse MapPermission(PermissionDefinition definition, bool defaultAllowed,
        ArchivePermissionEffect? Override, bool allowed);
}
