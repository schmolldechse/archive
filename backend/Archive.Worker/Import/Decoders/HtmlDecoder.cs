using Archive.Core.Entities;
using Archive.Core.Storage;

namespace Archive.Worker.Import.Decoders;

public sealed class HtmlDecoder : IUploadedDocumentDecoder
{
    public SourceType SourceType => SourceType.HtmlFile;

    public async Task<ImportedPage> DecodeAsync(Stream source, ImportLimits limits, CancellationToken cancellationToken)
    {
        var bytes = await ImportStream.ReadAllBytesAsync(source, limits.MaxBytes, cancellationToken);
        if (bytes.Length == 0)
            throw new InvalidDataException("The uploaded HTML is empty.");
        return new ImportedPage(SnapshotTextEncoding.Decode(bytes, "text/html", out _), null, [], []);
    }
}
