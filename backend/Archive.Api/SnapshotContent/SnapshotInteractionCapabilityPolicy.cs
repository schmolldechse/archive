using System.Text.RegularExpressions;
using Archive.Api.Options;

namespace Archive.Api.SnapshotContent;

/// <summary>Fails closed unless a structured Chromium brand/version hint matches the tested interaction matrix.</summary>
public sealed partial class SnapshotInteractionCapabilityPolicy(SnapshotContentOptions options)
{
    public bool AllowsInteraction(string? secChUa)
    {
        if (string.IsNullOrWhiteSpace(secChUa))
            return false;

        var matches = BrandVersion().Matches(secChUa);
        var remainder = BrandVersion().Replace(secChUa, string.Empty).Replace(",", string.Empty,
            StringComparison.Ordinal).Trim();
        if (matches.Count == 0 || remainder.Length != 0)
            return false;

        var versions = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match match in matches)
        {
            var brand = match.Groups["brand"].Value.Replace("\\\"", "\"", StringComparison.Ordinal);
            if (!int.TryParse(match.Groups["version"].Value, out var version) ||
                versions.TryGetValue(brand, out var existing) && existing != version)
                return false;
            versions[brand] = version;
        }

        return options.AllowedBrands.Any(brand =>
            versions.TryGetValue(brand, out var version) && version >= options.MinimumChromiumMajorVersion);
    }

    [GeneratedRegex("\\s*\\\"(?<brand>(?:\\\\.|[^\\\"])*)\\\"\\s*;\\s*v\\s*=\\s*\\\"(?<version>\\d+)\\\"\\s*",
        RegexOptions.CultureInvariant)]
    private static partial Regex BrandVersion();
}
