namespace Archive.Core.Storage;

/// <summary>Retains only responsive image candidates that are available in the archived snapshot.</summary>
public static class SnapshotSourceSet
{
    /// <summary>Rewrites mapped candidates and drops uncaptured remote candidates when a local one exists.</summary>
    public static string Rewrite(string value, Func<string, string?> resolve)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(resolve);
        var mappedCandidates = new List<string>();
        var hasMappedCandidate = false;
        foreach (var (url, descriptor) in ParseCandidates(value))
        {
            var rewrittenUrl = resolve(url);
            if (rewrittenUrl is null && url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                rewrittenUrl = url;
            if (rewrittenUrl is null)
                continue;

            hasMappedCandidate = true;
            mappedCandidates.Add(descriptor.Length == 0 ? rewrittenUrl : $"{rewrittenUrl} {descriptor}");
        }

        return hasMappedCandidate ? string.Join(", ", mappedCandidates) : value;
    }

    private static IEnumerable<(string Url, string Descriptor)> ParseCandidates(string value)
    {
        var position = 0;
        while (position < value.Length)
        {
            while (position < value.Length && (char.IsWhiteSpace(value[position]) || value[position] == ','))
                position++;
            if (position == value.Length)
                yield break;

            var urlStart = position;
            while (position < value.Length && !char.IsWhiteSpace(value[position]))
                position++;

            var url = value[urlStart..position];
            var trailingCommas = url.Length - url.TrimEnd(',').Length;
            if (trailingCommas > 0)
            {
                url = url[..^trailingCommas];
                if (url.Length > 0)
                    yield return (url, string.Empty);
                continue;
            }

            while (position < value.Length && char.IsWhiteSpace(value[position]))
                position++;
            var descriptorStart = position;
            while (position < value.Length && value[position] != ',')
                position++;
            var descriptor = value[descriptorStart..position].Trim();
            yield return (url, descriptor);
        }
    }
}
