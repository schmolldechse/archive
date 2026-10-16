using Archive.Core.Entities;

namespace Archive.Worker.Import;

public interface IUploadedDocumentDecoder
{
    SourceType SourceType { get; }
    Task<ImportedPage> DecodeAsync(Stream source, ImportLimits limits, CancellationToken cancellationToken);
}
