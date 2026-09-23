using System.Text;
using Archive.Core.Entities;

namespace Archive.Worker.Import;

public sealed class HtmlDecoder : IUploadedDocumentDecoder
{
    public SourceType SourceType => SourceType.HtmlFile;

    public async Task<ImportedPage> DecodeAsync(Stream source, ImportLimits limits, CancellationToken cancellationToken)
    {
        var bytes = await ImportStream.ReadAllBytesAsync(source, limits.MaxBytes, cancellationToken);
        if (bytes.Length == 0)
            throw new InvalidDataException("The uploaded HTML is empty.");
        return new ImportedPage(Encoding.UTF8.GetString(bytes), null, [], []);
    }
}
