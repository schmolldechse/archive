using Archive.SnapshotMigrator;

namespace Archive.Tests;

public sealed class SnapshotMigratorOptionsTests
{
    [Fact]
    public void UpdateAcceptsOneSnapshotSelector()
    {
        var snapshotId = Guid.NewGuid();

        var parsed = SnapshotMigratorOptions.TryParse(
            ["--update", "--snapshot", snapshotId.ToString(), "--json"], out var options, out var error);

        Assert.True(parsed, error);
        Assert.Equal(SnapshotMigratorAction.Update, options!.Action);
        Assert.Equal(snapshotId, options.SnapshotId);
        Assert.False(options.All);
        Assert.True(options.Json);
    }

    [Fact]
    public void CleanupRequiresExplicitConfirmation()
    {
        var parsed = SnapshotMigratorOptions.TryParse(
            ["--cleanup", "--all"], out var options, out var error);

        Assert.False(parsed);
        Assert.Null(options);
        Assert.Contains("--confirm", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--dry-run", "--all", "--snapshot", "00000000-0000-0000-0000-000000000001")]
    [InlineData("--update", "--all", "--restore")]
    [InlineData("--update", "--snapshot", "00000000-0000-0000-0000-000000000001", "--snapshot", "00000000-0000-0000-0000-000000000002")]
    public void MutuallyExclusiveArgumentsAreRejected(params string[] arguments)
    {
        var parsed = SnapshotMigratorOptions.TryParse(arguments, out var options, out _);

        Assert.False(parsed);
        Assert.Null(options);
    }
}
