namespace Archive.Core.Jobs;

public static class UrlNormalizer
{
    public static bool TryNormalizeSource(string? value, out string normalized, out string error)
    {
        normalized = string.Empty;
        error = string.Empty;

        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "Provide a valid HTTP or HTTPS URL.";
            return false;
        }

        normalized = Normalize(uri, includeQuery: true);
        return true;
    }

    public static string ProjectKey(string normalizedSource)
    {
        var uri = new Uri(normalizedSource);
        var path = uri.AbsolutePath.TrimEnd('/');
        return $"{uri.Host.ToLowerInvariant()}:{uri.Port}{(string.IsNullOrEmpty(path) ? "/" : path)}";
    }

    private static string Normalize(Uri uri, bool includeQuery)
    {
        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant(),
            Fragment = string.Empty,
            Path = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath.TrimEnd('/')
        };

        if (!includeQuery)
            builder.Query = string.Empty;

        return builder.Uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.PathAndQuery, UriFormat.UriEscaped);
    }
}
