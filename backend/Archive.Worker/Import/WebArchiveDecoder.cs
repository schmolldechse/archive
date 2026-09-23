using System.Text;
using System.Text.RegularExpressions;
using Archive.Core.Entities;

namespace Archive.Worker.Import;

public sealed class WebArchiveDecoder : IUploadedDocumentDecoder
{
    public SourceType SourceType => SourceType.WebArchiveFile;

    public async Task<ImportedPage> DecodeAsync(Stream source, ImportLimits limits, CancellationToken cancellationToken)
    {
        var bytes = await ImportStream.ReadAllBytesAsync(source, limits.MaxBytes, cancellationToken);
        var root = PropertyListReader.Read(bytes, limits);
        var resourceCount = 0;
        return ReadArchive(root, limits, 0, ref resourceCount);
    }

    private static ImportedPage ReadArchive(Dictionary<string, object?> archive, ImportLimits limits, int depth,
        ref int resourceCount)
    {
        if (depth > limits.MaxDepth)
            throw new InvalidDataException("The Webarchive has too many nested frames.");
        var main = GetDictionary(archive, "WebMainResource", required: true)!;
        var contentType = GetString(main, "WebResourceMIMEType", required: true)!;
        if (!contentType.Split(';', 2)[0].Trim().Equals("text/html", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The Webarchive main resource must be text/html.");
        var htmlBytes = GetBytes(main, "WebResourceData", required: true)!;
        if (htmlBytes.Length == 0)
            throw new InvalidDataException("The Webarchive main resource is empty.");
        var html = DecodeText(htmlBytes, GetString(main, "WebResourceTextEncodingName"));
        var baseUrl = GetString(main, "WebResourceURL");
        var frameName = GetString(main, "WebResourceFrameName");

        var resources = new List<ImportedResource>();
        var seen = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var value in GetArray(archive, "WebSubresources"))
        {
            if (value is not Dictionary<string, object?> item)
                throw new InvalidDataException("A Webarchive subresource is invalid.");
            var url = GetString(item, "WebResourceURL", required: true)!;
            var mime = GetString(item, "WebResourceMIMEType", required: true)!;
            var body = GetBytes(item, "WebResourceData", required: true)!;
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidDataException("A Webarchive subresource has no URL.");
            if (seen.TryGetValue(url, out var earlier))
            {
                if (!earlier.AsSpan().SequenceEqual(body))
                    throw new InvalidDataException("A Webarchive has conflicting resources for one URL.");
                continue;
            }
            seen.Add(url, body);
            if (++resourceCount > limits.MaxResources)
                throw new InvalidDataException("The Webarchive exceeds the resource limit.");
            resources.Add(new ImportedResource(url, mime, body,
                GetString(item, "WebResourceTextEncodingName")));
        }

        var frames = new List<ImportedPage>();
        foreach (var value in GetArray(archive, "WebSubframeArchives"))
        {
            if (value is not Dictionary<string, object?> frame)
                throw new InvalidDataException("A Webarchive subframe is invalid.");
            frames.Add(ReadArchive(frame, limits, depth + 1, ref resourceCount));
        }
        return new ImportedPage(html, baseUrl, resources, frames, frameName);
    }

    private static string DecodeText(byte[] bytes, string? encodingName)
    {
        try
        {
            Encoding encoding;
            var offset = 0;
            if (!string.IsNullOrWhiteSpace(encodingName))
                encoding = Encoding.GetEncoding(encodingName);
            else if (bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
            {
                encoding = Encoding.UTF8;
                offset = 3;
            }
            else if (bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE }))
            {
                encoding = Encoding.Unicode;
                offset = 2;
            }
            else if (bytes.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }))
            {
                encoding = Encoding.BigEndianUnicode;
                offset = 2;
            }
            else
            {
                var head = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 4_096));
                var charset = Regex.Match(head, "<meta\\b[^>]*\\bcharset\\s*=\\s*[\"']?(?<name>[A-Za-z0-9._-]+)",
                    RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                encoding = charset.Success ? Encoding.GetEncoding(charset.Groups["name"].Value) : Encoding.UTF8;
            }
            encoding = (Encoding)encoding.Clone();
            encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
            return encoding.GetString(bytes, offset, bytes.Length - offset).TrimStart('\uFEFF');
        }
        catch (Exception exception) when (exception is ArgumentException or DecoderFallbackException or NotSupportedException)
        {
            throw new InvalidDataException("The Webarchive text encoding is invalid or unsupported.", exception);
        }
    }

    private static Dictionary<string, object?>? GetDictionary(Dictionary<string, object?> source, string key,
        bool required = false)
    {
        if (source.TryGetValue(key, out var value) && value is Dictionary<string, object?> result)
            return result;
        if (required)
            throw new InvalidDataException($"The Webarchive is missing {key}.");
        return null;
    }

    private static string? GetString(Dictionary<string, object?> source, string key, bool required = false)
    {
        if (source.TryGetValue(key, out var value) && value is string result)
            return result;
        if (required)
            throw new InvalidDataException($"The Webarchive is missing {key}.");
        return null;
    }

    private static byte[]? GetBytes(Dictionary<string, object?> source, string key, bool required = false)
    {
        if (source.TryGetValue(key, out var value) && value is byte[] result)
            return result;
        if (required)
            throw new InvalidDataException($"The Webarchive is missing {key}.");
        return null;
    }

    private static IEnumerable<object?> GetArray(Dictionary<string, object?> source, string key)
    {
        if (!source.TryGetValue(key, out var value))
            return [];
        return value as List<object?>
            ?? throw new InvalidDataException($"The Webarchive {key} is not an array.");
    }
}
