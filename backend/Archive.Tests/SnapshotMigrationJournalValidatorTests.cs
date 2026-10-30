using Archive.SnapshotMigrator;

namespace Archive.Tests;

public sealed class SnapshotMigrationJournalValidatorTests
{
    [Fact]
    public void AcceptsJournalWhoseObjectsStayInsideBothGenerations()
    {
        var snapshotId = Guid.NewGuid();
        var migrationId = Guid.NewGuid();
        var journal = CreateJournal(snapshotId, migrationId);

        SnapshotMigrationJournalValidator.Validate(journal, snapshotId);
    }

    [Fact]
    public void RejectsObjectOutsideTheJournaledGeneration()
    {
        var snapshotId = Guid.NewGuid();
        var migrationId = Guid.NewGuid();
        var journal = CreateJournal(snapshotId, migrationId) with
        {
            MigratedObjectKeys = ["snapshots/other/index.html"]
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            SnapshotMigrationJournalValidator.Validate(journal, snapshotId));

        Assert.Contains("migrated object keys", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsJournalForAnotherSnapshot()
    {
        var snapshotId = Guid.NewGuid();
        var journal = CreateJournal(snapshotId, Guid.NewGuid());

        var exception = Assert.Throws<InvalidDataException>(() =>
            SnapshotMigrationJournalValidator.Validate(journal, Guid.NewGuid()));

        Assert.Contains("identity", exception.Message, StringComparison.Ordinal);
    }

    private static SnapshotMigrationJournal CreateJournal(Guid snapshotId, Guid migrationId)
    {
        var originalPrefix = $"snapshots/{snapshotId:N}";
        var migratedPrefix = $"{originalPrefix}/generations/manifest-v4/{migrationId:N}";
        return new()
        {
            SnapshotId = snapshotId,
            MigrationId = migrationId,
            Status = SnapshotMigrationJournal.Prepared,
            OriginalContentPrefix = originalPrefix,
            MigratedContentPrefix = migratedPrefix,
            OriginalStorageBytes = 10,
            MigratedStorageBytes = 20,
            OriginalResourceCount = 1,
            MigratedResourceCount = 1,
            OriginalObjectKeys = [$"{originalPrefix}/index.html"],
            MigratedObjectKeys = [$"{migratedPrefix}/index.html"],
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
