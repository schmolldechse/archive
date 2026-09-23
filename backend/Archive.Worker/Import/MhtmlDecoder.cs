using Archive.Core.Entities;
using MimeKit;

namespace Archive.Worker.Import;

public sealed class MhtmlDecoder : IUploadedDocumentDecoder
{
    public SourceType SourceType => SourceType.MhtmlFile;

    public async Task<ImportedPage> DecodeAsync(Stream source, ImportLimits limits, CancellationToken cancellationToken)
    {
        var bytes = await ImportStream.ReadAllBytesAsync(source, limits.MaxBytes, cancellationToken);
        try
        {
            await using var input = new MemoryStream(bytes, writable: false);
            using var message = await MimeMessage.LoadAsync(input, cancellationToken);
            if (message.Body is not MultipartRelated related)
                throw new InvalidDataException("The MHTML root must be multipart/related.");
            var parts = message.BodyParts.OfType<MimePart>().ToArray();
            if (parts.Length > limits.MaxResources + 1)
                throw new InvalidDataException("The MHTML exceeds the resource limit.");

            var declaredRoot = message.Headers["Snapshot-Content-Location"]?.Trim();
            var htmlParts = parts.Where(x => x.ContentType.IsMimeType("text", "html")).ToArray();
            var root = related.Root is MimePart declaredPart &&
                declaredPart.ContentType.IsMimeType("text", "html") &&
                !string.Equals(declaredPart.ContentLocation?.Scheme, "chrome-error", StringComparison.OrdinalIgnoreCase)
                ? declaredPart : null;
            root ??= htmlParts.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(declaredRoot) &&
                string.Equals(x.ContentLocation?.ToString(), declaredRoot, StringComparison.Ordinal))
                ?? htmlParts.FirstOrDefault(x =>
                    !string.Equals(x.ContentLocation?.Scheme, "chrome-error", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException("The MHTML contains no usable HTML root.");
            var rootText = root as TextPart ?? throw new InvalidDataException("The MHTML root cannot be decoded as text.");
            var resources = new List<ImportedResource>();
            var frames = new List<ImportedPage>();
            var index = 0;
            foreach (var part in parts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (ReferenceEquals(part, root))
                    continue;
                var location = part.ContentLocation?.ToString();
                var cid = string.IsNullOrWhiteSpace(part.ContentId) ? null : $"cid:{part.ContentId}";
                var key = location ?? cid ?? $"urn:mhtml:part:{index++}";
                if (part.ContentType.IsMimeType("text", "html") && part is TextPart frameText)
                {
                    frames.Add(new ImportedPage(frameText.Text, location, [], []));
                    continue;
                }
                await using var decoded = new MemoryStream();
                if (part.Content is null)
                    throw new InvalidDataException("An MHTML part has no content.");
                await part.Content.DecodeToAsync(decoded, cancellationToken);
                if (decoded.Length > limits.MaxBytes)
                    throw new InvalidDataException("An MHTML part exceeds the import byte limit.");
                resources.Add(new ImportedResource(key, part.ContentType.MimeType, decoded.ToArray(),
                    part.ContentType.Charset, cid is null ? null : [cid]));
            }
            return new ImportedPage(rootText.Text, root.ContentLocation?.ToString() ?? declaredRoot,
                resources, frames);
        }
        catch (Exception exception) when (exception is FormatException or ParseException)
        {
            throw new InvalidDataException("The MHTML file is invalid.", exception);
        }
    }
}
