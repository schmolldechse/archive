using Archive.Core.Models.Snapshots;

namespace Archive.Tests;

public sealed class SnapshotByteRangeTests
{
    [Theory]
    [InlineData("bytes=0-3", 10, 0, 3)]
    [InlineData("bytes=4-", 10, 4, 9)]
    [InlineData("bytes=-4", 10, 6, 9)]
    [InlineData("bytes=0-99", 10, 0, 9)]
    [InlineData("bytes=-99", 10, 0, 9)]
    public void ResolvesSingleByteRanges(string value, long total, long start, long end)
    {
        Assert.True(SnapshotByteRange.TryResolve(value, total, out var range));
        Assert.Equal(start, range.Start);
        Assert.Equal(end, range.End);
        Assert.Equal(end - start + 1, range.Length);
    }

    [Theory]
    [InlineData("items=0-1")]
    [InlineData("bytes=10-11")]
    [InlineData("bytes=4-3")]
    [InlineData("bytes=-0")]
    [InlineData("bytes=0-1,4-5")]
    [InlineData("bytes=+1-2")]
    [InlineData("bytes=invalid")]
    public void RejectsUnsupportedOrUnsatisfiableRanges(string value) =>
        Assert.False(SnapshotByteRange.TryResolve(value, 10, out _));
}
