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
    [MapPropertyFromSource(nameof(PublicUserResponse.ProfileImageUrl), Use = nameof(GetProfileImageUrl))]
    public partial PublicUserResponse ToResponse(PublicUser user);

    private string? GetProfileImageUrl(PublicUser user) => user.GetProfileImageUrl(options.PublicApiOrigin);
}
