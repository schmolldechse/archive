using Archive.Core.Entities;

namespace Archive.Worker.Import;

public sealed record ImportLimits(long MaxBytes, int MaxResources, int MaxDepth, int MaxObjects);

public sealed record ImportedResource(
    string Url,
    string ContentType,
    byte[] Body,
    string? TextEncoding = null,
    IReadOnlyList<string>? Aliases = null);

public sealed record ImportedPage(
    string Html,
    string? BaseUrl,
    IReadOnlyList<ImportedResource> Resources,
    IReadOnlyList<ImportedPage> Frames,
    string? FrameName = null);

public interface IUploadedDocumentDecoder
{
    SourceType SourceType { get; }
    Task<ImportedPage> DecodeAsync(Stream source, ImportLimits limits, CancellationToken cancellationToken);
}
