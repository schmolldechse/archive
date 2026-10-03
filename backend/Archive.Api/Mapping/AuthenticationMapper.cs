using Archive.Api.Authentication.Catalog;
using Archive.Api.Contracts;
using Archive.Core.Entities.Authentication;
using Riok.Mapperly.Abstractions;

namespace Archive.Api.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class AuthenticationMapper
{
    [MapperIgnoreSource(nameof(ArchiveUser.ProfileImage))]
    [MapperIgnoreSource(nameof(ArchiveUser.CreatedAt))]
    [MapperIgnoreSource(nameof(ArchiveUser.UpdatedAt))]
    [MapperIgnoreSource(nameof(ArchiveUser.Identities))]
    public partial UserResponse ToResponse(ArchiveUser account, string? profileImageUrl);

    [MapperIgnoreSource(nameof(AuthProviderDescriptor.Scheme))]
    public partial AuthenticationProviderResponse ToResponse(AuthProviderDescriptor provider, string loginUrl);

    [MapperIgnoreTarget(nameof(ArchiveUser.Id))]
    [MapperIgnoreTarget(nameof(ArchiveUser.ProfileImage))]
    [MapperIgnoreTarget(nameof(ArchiveUser.CreatedAt))]
    [MapperIgnoreTarget(nameof(ArchiveUser.UpdatedAt))]
    [MapperIgnoreTarget(nameof(ArchiveUser.Identities))]
    public partial ArchiveUser ToModel(UpdateUserProfileRequest request);
}
