using Archive.Api.Contracts;
using Archive.Api.Contracts.Archive.Creation;
using Archive.Core.Models.Archive;
using Riok.Mapperly.Abstractions;

namespace Archive.Api.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class ArchiveMapper(ArchiveCreatorMapper creatorMapper)
{
    [UseMapper]
    private ArchiveCreatorMapper CreatorMapper { get; } = creatorMapper;

    public partial ArchiveResponse ToResponse(ArchiveStatus archive);

    [MapProperty(nameof(CreateArchiveUrlRequest.SourceUrl), nameof(ArchiveCreationRequest.OriginUrl))]
    public partial ArchiveCreationRequest ToRequest(CreateArchiveUrlRequest request, Guid? createdByUserId);

    [MapperIgnoreSource(nameof(CreateArchiveFileRequest.File))]
    [MapProperty(nameof(CreateArchiveFileRequest.OriginalLink), nameof(ArchiveCreationRequest.OriginUrl))]
    public partial ArchiveCreationRequest ToRequest(CreateArchiveFileRequest request, Guid? createdByUserId);
}
