using Archive.Core.Jobs;

namespace Archive.Tests;

public sealed class SnapshotCrawlUrlTests
{
    private static readonly Uri EffectiveOrigin = new("https://example.test/start");

    [Theory]
    [InlineData("https://example.test/a", "https://example.test/a")]
    [InlineData("https://example.test/a/", "https://example.test/a")]
    [InlineData("https://example.test/a?view=full#section", "https://example.test/a?view=full")]
    public void NormalizesSameOriginRoutes(string value, string expected)
    {
        Assert.True(SnapshotCrawlUrl.TryNormalizeInScope(value, EffectiveOrigin, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("http://example.test/a")]
    [InlineData("https://www.example.test/a")]
    [InlineData("https://example.test:8443/a")]
    [InlineData("mailto:archive@example.test")]
    [InlineData("javascript:void(0)")]
    public void RejectsRoutesOutsideEffectiveOrigin(string value) =>
        Assert.False(SnapshotCrawlUrl.TryNormalizeInScope(value, EffectiveOrigin, out _));
}
