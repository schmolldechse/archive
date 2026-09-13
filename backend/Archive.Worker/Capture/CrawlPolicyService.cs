using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Archive.Worker.Capture;

public sealed class CrawlPolicyService : IDisposable
{
    private readonly CrawlPolicyOptions _options;
    private readonly ArchiveBrowserOptions _browserOptions;
    private readonly WebBotAuthSigner _signer;
    private readonly HostRateLimiter _rateLimiter;
    private readonly PublicNetworkGuard _networkGuard = new();
    private readonly HttpClient _client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.All,
        ConnectTimeout = TimeSpan.FromSeconds(10)
    });

    public CrawlPolicyService(CrawlPolicyOptions options, ArchiveBrowserOptions browserOptions,
        WebBotAuthSigner signer, HostRateLimiter rateLimiter)
    {
        _options = options;
        _browserOptions = browserOptions;
        _signer = signer;
        _rateLimiter = rateLimiter;
        _client.Timeout = options.RequestTimeout;
    }

    public async Task EnsureAllowedAsync(Uri target, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return;

        var policy = await FetchPolicyAsync(target, cancellationToken);
        if (!policy.IsAllowed(target.PathAndQuery))
            throw new CaptureFailedException(
                $"robots.txt untersagt dem Archivierungsbot den Abruf von '{target.AbsolutePath}'.");

        await _rateLimiter.WaitAsync(target, policy.CrawlDelay, cancellationToken);
    }

    private async Task<RobotsPolicy> FetchPolicyAsync(Uri target, CancellationToken cancellationToken)
    {
        var current = new Uri(target.GetLeftPart(UriPartial.Authority) + "/robots.txt");
        for (var redirect = 0; redirect <= 3; redirect++)
        {
            if (!await _networkGuard.IsAllowedAsync(current.AbsoluteUri, cancellationToken))
                throw new CaptureFailedException("Das robots.txt-Netzwerkziel ist nicht öffentlich oder nicht zulässig.");

            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.TryAddWithoutValidation("User-Agent", _browserOptions.UserAgent);
            foreach (var (name, value) in _signer.CreateHeaders(current))
                request.Headers.TryAddWithoutValidation(name, value);

            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if ((int)response.StatusCode is >= 300 and < 400)
            {
                if (redirect == 3 || response.Headers.Location is null)
                    throw new CaptureFailedException("robots.txt enthält eine unzulässige Weiterleitungskette.");
                current = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(current, response.Headers.Location);
                continue;
            }

            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Gone)
                return RobotsPolicy.AllowAll;
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return RobotsPolicy.DisallowAll;
            if (!response.IsSuccessStatusCode)
                throw new CaptureFailedException($"robots.txt konnte nicht sicher geprüft werden (HTTP {(int)response.StatusCode}).");

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var content = await ReadLimitedAsync(stream, _options.MaxRobotsBytes, cancellationToken);
            return RobotsPolicy.Parse(content, _options.UserAgentToken);
        }

        throw new CaptureFailedException("robots.txt konnte nicht geprüft werden.");
    }

    private static async Task<string> ReadLimitedAsync(Stream stream, int maxBytes,
        CancellationToken cancellationToken)
    {
        using var output = new MemoryStream();
        var buffer = new byte[16_384];
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
                break;
            if (output.Length + read > maxBytes)
                throw new CaptureFailedException("robots.txt überschreitet das zulässige Größenlimit.");
            output.Write(buffer, 0, read);
        }
        return Encoding.UTF8.GetString(output.ToArray());
    }

    public void Dispose() => _client.Dispose();

    private sealed record RobotsPolicy(IReadOnlyList<Rule> Rules, TimeSpan CrawlDelay)
    {
        public static RobotsPolicy AllowAll { get; } = new([], TimeSpan.Zero);
        public static RobotsPolicy DisallowAll { get; } = new([new Rule(false, "/")], TimeSpan.Zero);

        public bool IsAllowed(string pathAndQuery)
        {
            var match = Rules.Where(rule => rule.Matches(pathAndQuery))
                .OrderByDescending(rule => rule.MatchLength)
                .ThenByDescending(rule => rule.Allow)
                .FirstOrDefault();
            return match?.Allow ?? true;
        }

        public static RobotsPolicy Parse(string content, string botToken)
        {
            var groups = new List<Group>();
            Group? group = null;
            var directivesStarted = false;

            foreach (var rawLine in content.Replace("\r", "", StringComparison.Ordinal).Split('\n'))
            {
                var line = rawLine.Split('#', 2)[0].Trim();
                var separator = line.IndexOf(':');
                if (separator <= 0)
                    continue;
                var name = line[..separator].Trim().ToLowerInvariant();
                var value = line[(separator + 1)..].Trim();

                if (name == "user-agent")
                {
                    if (group is null || directivesStarted)
                    {
                        group = new Group();
                        groups.Add(group);
                        directivesStarted = false;
                    }
                    group.Agents.Add(value);
                    continue;
                }
                if (group is null || group.Agents.Count == 0)
                    continue;

                directivesStarted = true;
                if (name is "allow" or "disallow")
                {
                    if (value.Length > 0)
                        group.Rules.Add(new Rule(name == "allow", value));
                }
                else if (name == "crawl-delay" &&
                         double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                             out var seconds) && seconds is >= 0 and <= 300)
                {
                    group.CrawlDelay = TimeSpan.FromSeconds(seconds);
                }
            }

            var candidates = groups.Select(group => new
                {
                    Group = group,
                    Match = group.Agents.Select(agent => AgentMatch(agent, botToken)).Max()
                })
                .Where(candidate => candidate.Match >= 0)
                .ToArray();
            if (candidates.Length == 0)
                return AllowAll;

            var best = candidates.Max(candidate => candidate.Match);
            var selected = candidates.Where(candidate => candidate.Match == best).Select(candidate => candidate.Group)
                .ToArray();
            return new RobotsPolicy(selected.SelectMany(group => group.Rules).ToArray(),
                selected.Max(group => group.CrawlDelay));
        }

        private static int AgentMatch(string pattern, string botToken)
        {
            pattern = pattern.Trim();
            if (pattern == "*")
                return 0;
            return botToken.Contains(pattern, StringComparison.OrdinalIgnoreCase) ? pattern.Length : -1;
        }

        private sealed class Group
        {
            public List<string> Agents { get; } = [];
            public List<Rule> Rules { get; } = [];
            public TimeSpan CrawlDelay { get; set; }
        }
    }

    private sealed record Rule(bool Allow, string Pattern)
    {
        public int MatchLength => Pattern.Count(character => character != '*');

        public bool Matches(string pathAndQuery)
        {
            var endAnchored = Pattern.EndsWith('$');
            var body = endAnchored ? Pattern[..^1] : Pattern;
            var expression = "^" + Regex.Escape(body).Replace("\\*", ".*", StringComparison.Ordinal) +
                (endAnchored ? "$" : "");
            return Regex.IsMatch(pathAndQuery, expression, RegexOptions.CultureInvariant);
        }
    }
}
