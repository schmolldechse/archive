namespace Archive.SnapshotMigrator;

/// <summary>Prevents a malformed migration journal from widening restore or deletion targets.</summary>
internal static class SnapshotMigrationJournalValidator
{
    public static void Validate(SnapshotMigrationJournal journal, Guid snapshotId)
    {
        ArgumentNullException.ThrowIfNull(journal);
        if (journal.SnapshotId != snapshotId || journal.MigrationId == Guid.Empty)
            throw new InvalidDataException("The migration journal identity is invalid.");
        if (journal.Status is not (SnapshotMigrationJournal.Prepared or SnapshotMigrationJournal.Applied or
                SnapshotMigrationJournal.Restored))
            throw new InvalidDataException("The migration journal status is invalid.");

        var snapshotPrefix = $"snapshots/{snapshotId:N}";
        var migratedPrefix = $"{snapshotPrefix}/generations/manifest-v4/{journal.MigrationId:N}";
        if (!IsSafePrefix(journal.OriginalContentPrefix, snapshotPrefix) ||
            !string.Equals(journal.MigratedContentPrefix, migratedPrefix, StringComparison.Ordinal) ||
            string.Equals(journal.OriginalContentPrefix, journal.MigratedContentPrefix, StringComparison.Ordinal))
            throw new InvalidDataException("The migration journal contains an invalid generation prefix.");
        if (journal.OriginalStorageBytes is < 0 || journal.MigratedStorageBytes < 0 ||
            journal.OriginalResourceCount is < 0 || journal.MigratedResourceCount < 0)
            throw new InvalidDataException("The migration journal contains invalid snapshot metadata.");

        ValidateObjectKeys(journal.OriginalObjectKeys, journal.OriginalContentPrefix, "original");
        ValidateObjectKeys(journal.MigratedObjectKeys, journal.MigratedContentPrefix, "migrated");
    }

    private static bool IsSafePrefix(string? value, string snapshotPrefix)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !(string.Equals(value, snapshotPrefix, StringComparison.Ordinal) ||
              value.StartsWith(snapshotPrefix + "/", StringComparison.Ordinal)))
            return false;
        return value[0] != '/' && !value.Contains('\\') && !value.Any(char.IsControl) &&
            value.Split('/').All(segment => segment is not ("" or "." or ".."));
    }

    private static void ValidateObjectKeys(IReadOnlyList<string>? keys, string prefix, string label)
    {
        if (keys is null || keys.Count == 0 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Count ||
            keys.Any(key => string.IsNullOrWhiteSpace(key) ||
                !key.StartsWith(prefix + "/", StringComparison.Ordinal) ||
                key.Length == prefix.Length + 1 || key.Contains('\\') || key.Any(char.IsControl) ||
                key[(prefix.Length + 1)..].Split('/').Any(segment => segment is "" or "." or "..")))
            throw new InvalidDataException($"The migration journal contains invalid {label} object keys.");
    }
}
