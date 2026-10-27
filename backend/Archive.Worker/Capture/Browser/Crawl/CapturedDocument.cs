using Archive.Core.Jobs;
using Archive.Core.Storage;

namespace Archive.Worker.Capture.Browser.Crawl;

internal sealed class CapturedDocument(
    string requestedUrl,
    string url,
    string contentType,
    int? statusCode,
    byte[] sourceBody,
    string fallbackHtml,
    IReadOnlyList<string> redirectChain,
    IReadOnlyList<string> discoveredLinks,
    byte[]? screenshot,
    long reservedBytes)
{
    private readonly HashSet<string> _aliases = new(StringComparer.Ordinal);

    public string RequestedUrl { get; } = requestedUrl;
    public string Url { get; } = url;
    public string ContentType { get; } = contentType;
    public int? StatusCode { get; } = statusCode;
    public byte[] SourceBody { get; } = sourceBody;
    public string FallbackHtml { get; } = fallbackHtml;
    public IReadOnlyList<string> RedirectChain { get; } = redirectChain;
    public IReadOnlyList<string> DiscoveredLinks { get; } = discoveredLinks;
    public byte[]? Screenshot { get; } = screenshot;
    public long ReservedBytes { get; } = reservedBytes;
    public IReadOnlyCollection<string> Aliases => _aliases;

    public void AddAliases(IEnumerable<string?> aliases)
    {
        foreach (var alias in aliases)
        {
            if (!UrlNormalizer.TryNormalizeSource(alias, out var normalized, out _) ||
                !Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
                continue;
            _aliases.Add(SnapshotResourcePath.ToAbsoluteUrlKey(uri));
        }
    }
}
