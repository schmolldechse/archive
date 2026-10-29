using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Archive.Core.Storage;
using Archive.Worker.Capture.Browser;
using Archive.Worker.Capture.Network;

namespace Archive.Worker.Import;

public sealed record ImportedMediaCompletion(ImportedPage Page, IReadOnlyList<string> MissingUrls);

/// <summary>Completes explicit finite media references that an uploaded offline archive did not contain.</summary>
public sealed class ImportedMediaCompleter : IDisposable
{
    private const int MaxRedirects = 3;
    private readonly ICaptureNetworkGuard _networkGuard;
    private readonly ILogger<ImportedMediaCompleter> _logger;
    private readonly HttpClient _client;

    public ImportedMediaCompleter(ICaptureNetworkGuard networkGuard, ArchiveBrowserOptions browserOptions,
        ILogger<ImportedMediaCompleter> logger)
    {
        _networkGuard = networkGuard;
        _logger = logger;
        _client = new HttpClient(new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            UseCookies = false
        })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        if (!string.IsNullOrWhiteSpace(browserOptions.UserAgent))
            _client.DefaultRequestHeaders.UserAgent.TryParseAdd(browserOptions.UserAgent);
    }

    public async Task<ImportedMediaCompletion> CompleteAsync(ImportedPage page, long maxAdditionalBytes,
        int maxAdditionalResources, CancellationToken cancellationToken)
    {
        var budget = new CompletionBudget(Math.Max(0, maxAdditionalBytes), Math.Max(0, maxAdditionalResources));
        var missing = new HashSet<string>(StringComparer.Ordinal);
        var completed = await CompletePageAsync(page, budget, missing, cancellationToken);
        return new ImportedMediaCompletion(completed, missing.Order(StringComparer.Ordinal).ToArray());
    }

    private async Task<ImportedPage> CompletePageAsync(ImportedPage page, CompletionBudget budget,
        HashSet<string> missing, CancellationToken cancellationToken)
    {
        var resources = page.Resources.ToList();
        var known = KnownUrls(page, resources);
        foreach (var candidate in DiscoverCandidates(page))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!known.Add(candidate.Url))
                continue;
            if (budget.RemainingResources == 0 || budget.RemainingBytes == 0)
            {
                missing.Add(candidate.Url);
                continue;
            }

            ImportedResource? resource;
            try
            {
                resource = await DownloadAsync(candidate, budget.RemainingBytes, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException)
            {
                _logger.LogDebug(exception, "Eingebettete Medienressource {Url} konnte nicht ergänzt werden",
                    candidate.Url);
                resource = null;
            }
            if (resource is null)
            {
                missing.Add(candidate.Url);
                continue;
            }

            resources.Add(resource);
            budget.Commit(resource.Body.LongLength);
            foreach (var alias in resource.Aliases ?? [])
                known.Add(alias);
        }

        var frames = new List<ImportedPage>(page.Frames.Count);
        foreach (var frame in page.Frames)
            frames.Add(await CompletePageAsync(frame, budget, missing, cancellationToken));
        return page with { Resources = resources, Frames = frames };
    }

    private async Task<ImportedResource?> DownloadAsync(MediaCandidate candidate, long remainingBytes,
        CancellationToken cancellationToken)
    {
        var current = new Uri(candidate.Url);
        for (var redirect = 0; redirect <= MaxRedirects; redirect++)
        {
            if (!await _networkGuard.IsAllowedAsync(current.AbsoluteUri, cancellationToken))
                return null;

            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.Referrer = null;
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (response.StatusCode is >= HttpStatusCode.MultipleChoices and < HttpStatusCode.BadRequest)
            {
                if (redirect == MaxRedirects || response.Headers.Location is null ||
                    !Uri.TryCreate(current, response.Headers.Location, out var target) ||
                    !IsHttp(target))
                    return null;
                current = target;
                continue;
            }

            if (response.StatusCode != HttpStatusCode.OK)
                return null;

            var contentType = NormalizeContentType(candidate, response.Content.Headers.ContentType?.MediaType);
            if (contentType is null || response.Content.Headers.ContentLength is > 0 and var declared &&
                declared > remainingBytes)
                return null;

            try
            {
                await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var output = new MemoryStream();
                var buffer = new byte[81_920];
                while (true)
                {
                    var read = await input.ReadAsync(buffer, cancellationToken);
                    if (read == 0)
                        break;
                    if (read > remainingBytes - output.Length)
                        return null;
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }

                var aliases = current.AbsoluteUri.Equals(candidate.Url, StringComparison.Ordinal)
                    ? null : new[] { current.AbsoluteUri };
                return new ImportedResource(candidate.Url, contentType, output.ToArray(), Aliases: aliases);
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException)
            {
                _logger.LogDebug(exception, "Eingebettete Medienressource {Url} konnte nicht ergänzt werden",
                    candidate.Url);
                return null;
            }
        }

        return null;
    }

    private static HashSet<string> KnownUrls(ImportedPage page, IEnumerable<ImportedResource> resources)
    {
        var known = new HashSet<string>(StringComparer.Ordinal);
        var baseUrl = SnapshotResourcePath.ResolveDocumentBase(page.Html, page.BaseUrl);
        foreach (var resource in resources)
        {
            AddResolved(known, baseUrl, resource.Url);
            foreach (var alias in resource.Aliases ?? [])
                AddResolved(known, baseUrl, alias);
        }
        return known;
    }

    private static IReadOnlyList<MediaCandidate> DiscoverCandidates(ImportedPage page)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(page.Html);
        var baseUrl = SnapshotResourcePath.ResolveDocumentBase(page.Html, page.BaseUrl);
        var candidates = new Dictionary<string, MediaCandidate>(StringComparer.Ordinal);
        foreach (var media in document.QuerySelectorAll("video, audio"))
        {
            var kind = media.LocalName.Equals("video", StringComparison.OrdinalIgnoreCase)
                ? MediaCandidateKind.Video : MediaCandidateKind.Audio;
            AddCandidate(candidates, baseUrl, media.GetAttribute("src"), kind);
            if (kind == MediaCandidateKind.Video)
                AddCandidate(candidates, baseUrl, media.GetAttribute("poster"), MediaCandidateKind.Poster);
            foreach (var source in media.QuerySelectorAll("source[src]"))
                AddCandidate(candidates, baseUrl, source.GetAttribute("src"), kind);
            foreach (var track in media.QuerySelectorAll("track[src]"))
                AddCandidate(candidates, baseUrl, track.GetAttribute("src"), MediaCandidateKind.TextTrack);
        }
        return candidates.Values.ToArray();
    }

    private static void AddCandidate(IDictionary<string, MediaCandidate> candidates, string? baseUrl, string? value,
        MediaCandidateKind kind)
    {
        if (!TryResolveHttp(baseUrl, value, out var url))
            return;
        candidates.TryAdd(url, new MediaCandidate(url, kind));
    }

    private static void AddResolved(ISet<string> urls, string? baseUrl, string value)
    {
        if (TryResolveHttp(baseUrl, value, out var url))
            urls.Add(url);
    }

    private static bool TryResolveHttp(string? baseUrl, string? value, out string url)
    {
        url = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        Uri? uri;
        if (!Uri.TryCreate(value, UriKind.Absolute, out uri) &&
            (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) || !Uri.TryCreate(baseUri, value, out uri)))
            return false;
        if (!IsHttp(uri))
            return false;
        url = SnapshotResourcePath.ToAbsoluteUrlKey(uri);
        return true;
    }

    private static bool IsHttp(Uri uri) =>
        uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
        uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeContentType(MediaCandidate candidate, string? contentType)
    {
        contentType = contentType?.Trim().ToLowerInvariant();
        if (candidate.Kind == MediaCandidateKind.Poster)
            return contentType?.StartsWith("image/", StringComparison.Ordinal) == true ? contentType : null;
        if (candidate.Kind == MediaCandidateKind.TextTrack)
            return contentType == "text/vtt" || contentType == "text/plain" &&
                candidate.Url.EndsWith(".vtt", StringComparison.OrdinalIgnoreCase) ? "text/vtt" : null;
        if (contentType?.StartsWith("video/", StringComparison.Ordinal) == true ||
            contentType?.StartsWith("audio/", StringComparison.Ordinal) == true)
            return contentType;
        if (contentType is not null and not "application/octet-stream")
            return null;

        var extension = Path.GetExtension(new Uri(candidate.Url).AbsolutePath).ToLowerInvariant();
        return candidate.Kind switch
        {
            MediaCandidateKind.Video => extension switch
            {
                ".mp4" or ".m4v" => "video/mp4",
                ".webm" => "video/webm",
                ".ogv" or ".ogg" => "video/ogg",
                _ => null
            },
            MediaCandidateKind.Audio => extension switch
            {
                ".mp3" => "audio/mpeg",
                ".m4a" or ".aac" => "audio/mp4",
                ".wav" => "audio/wav",
                ".oga" or ".ogg" => "audio/ogg",
                ".opus" => "audio/opus",
                ".flac" => "audio/flac",
                _ => null
            },
            _ => null
        };
    }

    public void Dispose() => _client.Dispose();

    private sealed record MediaCandidate(string Url, MediaCandidateKind Kind);
    private enum MediaCandidateKind { Video, Audio, Poster, TextTrack }

    private sealed class CompletionBudget(long remainingBytes, int remainingResources)
    {
        public long RemainingBytes { get; private set; } = remainingBytes;
        public int RemainingResources { get; private set; } = remainingResources;

        public void Commit(long byteCount)
        {
            RemainingBytes -= byteCount;
            RemainingResources--;
        }
    }
}
