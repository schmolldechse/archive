using System.Globalization;

namespace Archive.Core.Models.Snapshots;

/// <summary>Optional HTTP representation selection applied after snapshot and manifest authorization.</summary>
public sealed record SnapshotContentRequest(string? Range, string? IfRange)
{
    public static SnapshotContentRequest Full { get; } = new(null, null);
}

public sealed record SnapshotByteRange(long Start, long End)
{
    public long Length => checked(End - Start + 1);

    public static bool TryResolve(string value, long totalLength, out SnapshotByteRange range)
    {
        range = null!;
        if (totalLength <= 0 || string.IsNullOrWhiteSpace(value) ||
            !value.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase) || value.Contains(','))
            return false;

        var specifier = value[6..].Trim();
        var separator = specifier.IndexOf('-');
        if (separator < 0 || separator != specifier.LastIndexOf('-'))
            return false;

        var first = specifier[..separator].Trim();
        var last = specifier[(separator + 1)..].Trim();
        if (first.Length == 0)
        {
            if (!long.TryParse(last, NumberStyles.None, CultureInfo.InvariantCulture, out var suffixLength) ||
                suffixLength <= 0)
                return false;
            suffixLength = Math.Min(suffixLength, totalLength);
            range = new SnapshotByteRange(totalLength - suffixLength, totalLength - 1);
            return true;
        }

        if (!long.TryParse(first, NumberStyles.None, CultureInfo.InvariantCulture, out var start) ||
            start >= totalLength)
            return false;
        var end = totalLength - 1;
        if (last.Length > 0 &&
            (!long.TryParse(last, NumberStyles.None, CultureInfo.InvariantCulture, out end) || end < start))
            return false;
        range = new SnapshotByteRange(start, Math.Min(end, totalLength - 1));
        return true;
    }
}
