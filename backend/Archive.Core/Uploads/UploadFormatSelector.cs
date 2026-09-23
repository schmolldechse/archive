using System.Text;
using Archive.Core.Entities;

namespace Archive.Core.Uploads;

public interface IUploadFormatProbe
{
    SourceType SourceType { get; }
    string Extension { get; }
    string ContentType { get; }
    bool Matches(ReadOnlySpan<byte> prefix);
}

public sealed class UploadFormatSelector
{
    private readonly IReadOnlyDictionary<string, IUploadFormatProbe> _probes;

    public UploadFormatSelector(IEnumerable<IUploadFormatProbe> probes)
    {
        var values = probes.ToArray();
        if (values.Length < 3 || values.Select(x => x.SourceType).Distinct().Count() != values.Length ||
            values.Select(x => x.Extension).Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Length ||
            !new[] { SourceType.HtmlFile, SourceType.MhtmlFile, SourceType.WebArchiveFile }
                .All(type => values.Any(probe => probe.SourceType == type)))
            throw new InvalidOperationException("A unique upload probe is required for each supported file format.");
        _probes = values.ToDictionary(x => x.Extension, StringComparer.OrdinalIgnoreCase);
    }

    public IUploadFormatProbe? Select(string fileName, ReadOnlySpan<byte> prefix)
    {
        var extension = Path.GetExtension(fileName);
        return _probes.TryGetValue(extension, out var probe) && probe.Matches(prefix) ? probe : null;
    }
}

public sealed class HtmlUploadFormatProbe : IUploadFormatProbe
{
    public SourceType SourceType => SourceType.HtmlFile;
    public string Extension => ".html";
    public string ContentType => "text/html; charset=utf-8";
    public bool Matches(ReadOnlySpan<byte> prefix)
    {
        if (prefix.IsEmpty || prefix.StartsWith("bplist00"u8))
            return false;
        var text = Encoding.UTF8.GetString(prefix);
        return !text.Contains("<plist", StringComparison.OrdinalIgnoreCase) &&
               !(text.Contains("MIME-Version:", StringComparison.OrdinalIgnoreCase) &&
                 text.Contains("multipart/related", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class MhtmlUploadFormatProbe : IUploadFormatProbe
{
    public SourceType SourceType => SourceType.MhtmlFile;
    public string Extension => ".mhtml";
    public string ContentType => "multipart/related";
    public bool Matches(ReadOnlySpan<byte> prefix)
    {
        var headers = Encoding.ASCII.GetString(prefix);
        return headers.Contains("MIME-Version:", StringComparison.OrdinalIgnoreCase) &&
               headers.Contains("multipart/related", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class WebArchiveUploadFormatProbe : IUploadFormatProbe
{
    public SourceType SourceType => SourceType.WebArchiveFile;
    public string Extension => ".webarchive";
    public string ContentType => "application/x-webarchive";
    public bool Matches(ReadOnlySpan<byte> prefix)
    {
        if (prefix.StartsWith("bplist00"u8))
            return true;
        var text = Encoding.UTF8.GetString(prefix);
        return text.Contains("<plist", StringComparison.OrdinalIgnoreCase) &&
               text.Contains("WebMainResource", StringComparison.Ordinal);
    }
}
