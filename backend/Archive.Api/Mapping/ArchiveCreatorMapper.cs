using Archive.Api.Authentication;
using Archive.Api.Contracts.Snapshots;
using Archive.Core.Models.Users;
using Riok.Mapperly.Abstractions;

namespace Archive.Api.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class ArchiveCreatorMapper(ArchiveAuthenticationOptions options)
{
    [MapperIgnoreSource(nameof(ArchiveCreator.ProfileImage))]
    [MapPropertyFromSource(nameof(ArchiveCreatorResponse.ProfileImageUrl), Use = nameof(GetProfileImageUrl))]
    public partial ArchiveCreatorResponse ToResponse(ArchiveCreator creator);

    private string? GetProfileImageUrl(ArchiveCreator creator) => creator.GetProfileImageUrl(options.PublicApiOrigin);
}
