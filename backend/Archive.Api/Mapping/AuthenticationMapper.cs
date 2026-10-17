using Archive.Api.Authentication.Catalog;
using Archive.Api.Contracts.Authentication;
using Archive.Core.Entities.Authentication;
using Riok.Mapperly.Abstractions;

namespace Archive.Api.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class AuthenticationMapper
{
    [MapperIgnoreSource(nameof(ArchiveUser.PermissionOverrides))]
    [MapperIgnoreSource(nameof(ArchiveUser.ProfileImage))]
    [MapperIgnoreSource(nameof(ArchiveUser.Id))]
    [MapperIgnoreSource(nameof(ArchiveUser.CreatedAt))]
    [MapperIgnoreSource(nameof(ArchiveUser.UpdatedAt))]
    [MapperIgnoreSource(nameof(ArchiveUser.Identities))]
    [MapperIgnoreSource(nameof(ArchiveUser.RegistrationCompletedAt))]
    public partial UserProfileResponse ToResponse(ArchiveUser account, string? profileImageUrl);

    [MapperIgnoreSource(nameof(AuthenticationProviderDescriptor.Scheme))]
    public partial AuthenticationProviderResponse ToResponse(AuthenticationProviderDescriptor provider);

}
