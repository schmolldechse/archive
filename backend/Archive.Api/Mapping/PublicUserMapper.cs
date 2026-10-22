using Archive.Api.Authentication;
using Archive.Api.Contracts.Users;
using Archive.Core.Models.Users;
using Riok.Mapperly.Abstractions;

namespace Archive.Api.Mapping;

/// <summary>Maps shareable user projections to their public API representation.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class PublicUserMapper(ArchiveAuthenticationOptions options)
{
    [MapperIgnoreSource(nameof(PublicUser.HasProfileImage))]
    [MapPropertyFromSource(nameof(PublicUserResponse.ProfileImageUrl), Use = nameof(GetPublicProfileImageUrl))]
    public partial PublicUserResponse ToResponse(PublicUser user);

    [MapperIgnoreSource(nameof(UserListEntry.HasProfileImage))]
    [MapPropertyFromSource(nameof(PublicUserResponse.ProfileImageUrl), Use = nameof(GetListProfileImageUrl))]
    public partial PublicUserResponse ToResponse(UserListEntry user);

    private string? GetPublicProfileImageUrl(PublicUser user) => user.GetProfileImageUrl(options.PublicApiOrigin);

    private string? GetListProfileImageUrl(UserListEntry user) => user.HasProfileImage
        ? $"{options.PublicApiOrigin.TrimEnd('/')}/api/users/{Uri.EscapeDataString(user.Handle)}/profile-image"
        : null;
}
