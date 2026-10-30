using Archive.SnapshotMigrator;

namespace Archive.Tests;

public sealed class SnapshotMigratorOptionsTests
{
    [Fact]
    public void MigrateAcceptsOneSnapshotSelector()
    {
        var snapshotId = Guid.NewGuid();

        var parsed = SnapshotMigratorOptions.TryParse(
            ["--migrate", "--snapshot", snapshotId.ToString(), "--json"], out var options, out var error);

        Assert.True(parsed, error);
        Assert.Equal(SnapshotMigratorAction.Migrate, options!.Action);
        Assert.Equal(snapshotId, options.SnapshotId);
        Assert.False(options.All);
        Assert.True(options.Json);
    }

    [Fact]
    public void CleanRequiresExplicitConfirmation()
    {
        var parsed = SnapshotMigratorOptions.TryParse(
            ["--clean", "--all"], out var options, out var error);

        Assert.False(parsed);
        Assert.Null(options);
        Assert.Contains("--confirm", error, StringComparison.Ordinal);
    }

    [Fact]
    public void CleanAcceptsConfirmationForAllJournals()
    {
        var parsed = SnapshotMigratorOptions.TryParse(
            ["--clean", "--all", "--confirm", "--json"], out var options, out var error);

        Assert.True(parsed, error);
        Assert.Equal(SnapshotMigratorAction.Clean, options!.Action);
        Assert.True(options.All);
        Assert.True(options.Confirm);
        Assert.True(options.Json);
    }

    [Fact]
    public void ConfirmationIsRejectedForNonDestructiveActions()
    {
        var parsed = SnapshotMigratorOptions.TryParse(
            ["--dry-run", "--all", "--confirm"], out var options, out var error);

        Assert.False(parsed);
        Assert.Null(options);
        Assert.Contains("only valid with --clean", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void HelpStopsParsingWithoutAnError(string argument)
    {
        var parsed = SnapshotMigratorOptions.TryParse([argument], out var options, out var error);

        Assert.False(parsed);
        Assert.Null(options);
        Assert.Equal(string.Empty, error);
    }

    [Theory]
    [InlineData("--dry-run", "--all", "--snapshot", "00000000-0000-0000-0000-000000000001")]
    [InlineData("--migrate", "--all", "--restore")]
    [InlineData("--migrate", "--snapshot", "00000000-0000-0000-0000-000000000001", "--snapshot", "00000000-0000-0000-0000-000000000002")]
    public void MutuallyExclusiveArgumentsAreRejected(params string[] arguments)
    {
        var parsed = SnapshotMigratorOptions.TryParse(arguments, out var options, out _);

        Assert.False(parsed);
        Assert.Null(options);
    }
}
