using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace Archive.Core.Storage;

/// <summary>Decodes captured HTML and CSS before converting them to UTF-8 snapshot objects.</summary>
public static class SnapshotTextEncoding
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>Decodes text using a BOM, HTTP charset, HTML/CSS declaration, or strict UTF-8 fallback.</summary>
    public static string Decode(byte[] body, string contentType, out bool preserveUtf8Bom,
        string? declaredCharset = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        preserveUtf8Bom = body.AsSpan().StartsWith(Encoding.UTF8.Preamble);
        if (TryGetBomEncoding(body, out var bomEncoding, out var offset))
            return DecodeBytes(bomEncoding, body, offset, contentType);

        var isHtml = contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);
        var charset = string.IsNullOrWhiteSpace(declaredCharset)
            ? MediaTypeHeaderValue.TryParse(contentType, out var mediaType) ? mediaType.CharSet?.Trim('"') : null
            : declaredCharset.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(charset))
            charset = DetectDeclaredCharset(body, isHtml);
        if (string.IsNullOrWhiteSpace(charset)) return DecodeBytes(StrictUtf8, body, 0, contentType);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            var encoding = Encoding.GetEncoding(charset, EncoderFallback.ExceptionFallback,
                DecoderFallback.ExceptionFallback);
            return DecodeBytes(encoding, body, 0, contentType);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException($"Snapshot text is invalid for its declared charset: {charset}.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"Snapshot text uses an unsupported charset: {charset}.", exception);
        }
    }

    private static bool TryGetBomEncoding(byte[] body, out Encoding encoding, out int offset)
    {
        if (body.AsSpan().StartsWith(new byte[] { 0x00, 0x00, 0xFE, 0xFF }))
        {
            encoding = new UTF32Encoding(true, false, true);
            offset = 4;
        }
        else if (body.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE, 0x00, 0x00 }))
        {
            encoding = new UTF32Encoding(false, false, true);
            offset = 4;
        }
        else if (body.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }))
        {
            encoding = new UnicodeEncoding(true, false, true);
            offset = 2;
        }
        else if (body.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE }))
        {
            encoding = new UnicodeEncoding(false, false, true);
            offset = 2;
        }
        else if (body.AsSpan().StartsWith(Encoding.UTF8.Preamble))
        {
            encoding = StrictUtf8;
            offset = Encoding.UTF8.Preamble.Length;
        }
        else
        {
            encoding = StrictUtf8;
            offset = 0;
            return false;
        }
        return true;
    }

    private static string? DetectDeclaredCharset(byte[] body, bool isHtml)
    {
        var prefix = Encoding.ASCII.GetString(body, 0, Math.Min(body.Length, 4_096));
        if (!isHtml)
        {
            var css = Regex.Match(prefix, "^\\s*@charset\\s+[\\\"'](?<name>[A-Za-z0-9._-]+)[\\\"']",
                RegexOptions.IgnoreCase);
            return css.Success ? css.Groups["name"].Value : null;
        }

        foreach (Match meta in Regex.Matches(prefix, "<meta\\b[^>]*>", RegexOptions.IgnoreCase))
        {
            var direct = Regex.Match(meta.Value,
                "\\bcharset\\s*=\\s*[\\\"']?(?<name>[A-Za-z0-9._-]+)", RegexOptions.IgnoreCase);
            if (direct.Success) return direct.Groups["name"].Value;
            var httpEquiv = Regex.Match(meta.Value, "\\bhttp-equiv\\s*=\\s*[\\\"']?content-type",
                RegexOptions.IgnoreCase);
            if (!httpEquiv.Success) continue;
            var content = Regex.Match(meta.Value,
                "\\bcontent\\s*=\\s*[\\\"'][^\\\"']*?charset\\s*=\\s*(?<name>[A-Za-z0-9._-]+)",
                RegexOptions.IgnoreCase);
            if (content.Success) return content.Groups["name"].Value;
        }
        return null;
    }

    private static string DecodeBytes(Encoding encoding, byte[] body, int offset, string contentType)
    {
        try { return encoding.GetString(body, offset, body.Length - offset); }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException($"Snapshot text does not match {contentType} encoding.", exception);
        }
    }
}
