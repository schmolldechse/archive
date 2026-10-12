using Archive.Api.Authentication;
using Archive.Api.Contracts;
using Archive.Core.Models.Users;
using Riok.Mapperly.Abstractions;
using System.Security.Cryptography;

namespace Archive.Api.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class ArchiveCreatorMapper(ArchiveAuthenticationOptions options)
{
    [MapperIgnoreSource(nameof(ArchiveCreator.profileImage))]
    [MapPropertyFromSource(nameof(ArchiveCreatorResponse.ProfileImageUrl), Use = nameof(GetProfileImageUrl))]
    public partial ArchiveCreatorResponse ToResponse(ArchiveCreator creator);

    private string? GetProfileImageUrl(ArchiveCreator creator)
    {
        if (creator.profileImage is not { Length: > 0 } image)
            return null;

        var version = Convert.ToHexStringLower(SHA256.HashData(image));
        return $"{options.PublicApiOrigin.TrimEnd('/')}/api/users/{Uri.EscapeDataString(creator.Handle)}/profile-image?v={version}";
    }
}
