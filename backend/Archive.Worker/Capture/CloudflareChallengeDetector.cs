using Microsoft.Playwright;

namespace Archive.Worker.Capture;

internal static class CloudflareChallengeDetector
{
    public static bool IsChallengeResponse(IResponse response) => IsChallengeHeaders(response.Headers);

    public static bool IsChallengeHeaders(IEnumerable<KeyValuePair<string, string>> headers) => headers.Any(header =>
        header.Key.Equals("cf-mitigated", StringComparison.OrdinalIgnoreCase) &&
        header.Value.Trim().Equals("challenge", StringComparison.OrdinalIgnoreCase));

    public static bool IsChallengeResource(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (IsChallengePath(uri) || uri.Host.Equals("challenges.cloudflare.com", StringComparison.OrdinalIgnoreCase));

    public static bool IsChallengePath(Uri uri) =>
        uri.AbsolutePath.StartsWith("/cdn-cgi/challenge-platform/", StringComparison.OrdinalIgnoreCase);

    public static Task<DocumentState> InspectAsync(IPage page, CancellationToken cancellationToken) =>
        page.EvaluateAsync<DocumentState>("""
            () => {
                // JSD scripts and embedded Turnstile widgets also occur on real pages.
                // Require interstitial-specific evidence, not just the Cloudflare name.
                const options = window._cf_chl_opt;
                const challengeOptions = !!(options && (options.cType || options.cRay));
                const platformScript = [...document.scripts].some(s =>
                    s.src.includes('/cdn-cgi/challenge-platform/'));
                const title = document.title.trim();
                const challengeTitle = /^(just a moment|attention required|einen moment|checking your browser)/i.test(title);
                const form = document.querySelector('#challenge-form, #cf-challenge-form, #challenge-stage, #cf-challenge-running');
                const provider = challengeOptions || platformScript || !!document.querySelector('#cf-footer-text');
                return {
                    ready: !!document.body && document.readyState !== 'loading',
                    isChallenge: challengeOptions ||
                        (provider && (!!form || challengeTitle)) ||
                        (!!form && challengeTitle)
                };
            }
            """).WaitAsync(cancellationToken);

    public static Task<string> SnapshotHtmlAsync(IPage page, CancellationToken cancellationToken) =>
        page.EvaluateAsync<string>("""
            () => {
                // Clean a clone, leaving the live DOM (and screenshot) intact.
                const clone = document.documentElement.cloneNode(true);
                const isChallengeUrl = value => {
                    try {
                        const url = new URL(value, document.baseURI);
                        return url.pathname.toLowerCase().startsWith('/cdn-cgi/challenge-platform/') ||
                            url.hostname.toLowerCase() === 'challenges.cloudflare.com';
                    } catch { return false; }
                };
                clone.querySelectorAll('script, link[href], iframe[src]').forEach(element => {
                    const url = element.getAttribute('src') || element.getAttribute('href');
                    const injectedScript = element.tagName === 'SCRIPT' && !url &&
                        /window\.__CF\$cv\$params|window\._cf_chl_opt/.test(element.textContent || '');
                    if ((url && isChallengeUrl(url)) || injectedScript) element.remove();
                });
                const doctype = document.doctype ? new XMLSerializer().serializeToString(document.doctype) + '\n' : '';
                return doctype + clone.outerHTML;
            }
            """).WaitAsync(cancellationToken);

    internal sealed class DocumentState
    {
        public bool Ready { get; set; }
        public bool IsChallenge { get; set; }
    }
}
