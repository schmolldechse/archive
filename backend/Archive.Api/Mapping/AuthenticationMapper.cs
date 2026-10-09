using Archive.Api.Authentication.Catalog;
using Archive.Api.Contracts.Authentication;
using Archive.Core.Entities.Authentication;
using Riok.Mapperly.Abstractions;

namespace Archive.Api.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class AuthenticationMapper
{
    [MapperIgnoreSource(nameof(ArchiveUser.Id))]
    [MapperIgnoreSource(nameof(ArchiveUser.ProfileImage))]
    [MapperIgnoreSource(nameof(ArchiveUser.CreatedAt))]
    [MapperIgnoreSource(nameof(ArchiveUser.UpdatedAt))]
    [MapperIgnoreSource(nameof(ArchiveUser.Identities))]
    public partial AuthenticatedArchiveUser ToResponse(ArchiveUser account, string? profileImageUrl);

    [MapperIgnoreSource(nameof(AuthProviderDescriptor.Scheme))]
    public partial AuthenticationProvider ToResponse(AuthProviderDescriptor provider);

    [MapperIgnoreTarget(nameof(ArchiveUser.Id))]
    [MapperIgnoreTarget(nameof(ArchiveUser.ProfileImage))]
    [MapperIgnoreTarget(nameof(ArchiveUser.CreatedAt))]
    [MapperIgnoreTarget(nameof(ArchiveUser.UpdatedAt))]
    [MapperIgnoreTarget(nameof(ArchiveUser.Identities))]
    public partial ArchiveUser ToModel(UpdateUserProfile request);
}
