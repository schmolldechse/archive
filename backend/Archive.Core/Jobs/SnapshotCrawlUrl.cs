namespace Archive.Core.Jobs;

/// <summary>Normalizes navigable snapshot routes and keeps a crawl on one effective origin.</summary>
public static class SnapshotCrawlUrl
{
    /// <summary>Normalizes an absolute HTTP(S) candidate when it belongs to the supplied effective origin.</summary>
    public static bool TryNormalizeInScope(string? value, Uri effectiveOrigin, out string normalized)
    {
        ArgumentNullException.ThrowIfNull(effectiveOrigin);
        normalized = string.Empty;
        if (!UrlNormalizer.TryNormalizeSource(value, out var candidate, out _) ||
            !Uri.TryCreate(candidate, UriKind.Absolute, out var candidateUri) ||
            !IsSameOrigin(candidateUri, effectiveOrigin))
            return false;

        normalized = candidate;
        return true;
    }

    /// <summary>Returns whether two absolute HTTP(S) URLs have the same scheme, host, and effective port.</summary>
    public static bool IsSameOrigin(Uri left, Uri right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.IsAbsoluteUri && right.IsAbsoluteUri &&
            left.Scheme.Equals(right.Scheme, StringComparison.OrdinalIgnoreCase) &&
            left.IdnHost.Equals(right.IdnHost, StringComparison.OrdinalIgnoreCase) &&
            left.Port == right.Port;
    }
}
