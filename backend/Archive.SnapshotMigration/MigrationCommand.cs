namespace Archive.SnapshotMigration;

internal enum MigrationMode
{
    DryRun,
    Apply,
    Rollback,
    Cleanup,
    Help
}

internal sealed record MigrationCommand(MigrationMode Mode, int BatchSize = 100,
    int RetentionDays = 30, Guid? SnapshotId = null, bool ConfirmCleanup = false)
{
    public const string HelpText = """
        Snapshot resource migration
          (default)                 Inspect and validate legacy snapshots without writing.
          --apply                   Migrate snapshots to route-neutral resource paths.
          --rollback --snapshot-id ID
                                    Restore one snapshot's previous content prefix.
          --cleanup                 Report legacy objects eligible for deletion.
          --confirm-cleanup         Apply --cleanup (requires the retention period to have elapsed).
          --batch-size N            Batch size for migration (default: 100).
          --retention-days N        Legacy-object retention before cleanup (default: 30).
          --help                    Show this help.

        Configuration is read from the standard .NET configuration sources. Required values
        include ConnectionStrings:DefaultConnection and the ObjectStorage section.
        """;

    public static MigrationCommand Parse(string[] args)
    {
        var mode = MigrationMode.DryRun;
        var modeSpecified = false;
        var batchSize = 100;
        var retentionDays = 30;
        Guid? snapshotId = null;
        var confirmCleanup = false;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--help": mode = MigrationMode.Help; break;
                case "--dry-run": mode = SelectMode(mode, MigrationMode.DryRun, ref modeSpecified); break;
                case "--apply": mode = SelectMode(mode, MigrationMode.Apply, ref modeSpecified); break;
                case "--rollback": mode = SelectMode(mode, MigrationMode.Rollback, ref modeSpecified); break;
                case "--cleanup": mode = SelectMode(mode, MigrationMode.Cleanup, ref modeSpecified); break;
                case "--confirm-cleanup": confirmCleanup = true; break;
                case "--batch-size": batchSize = ReadPositiveInt(args, ref index, "--batch-size"); break;
                case "--retention-days": retentionDays = ReadPositiveInt(args, ref index, "--retention-days"); break;
                case "--snapshot-id":
                    if (++index >= args.Length || !Guid.TryParse(args[index], out var parsedId))
                        throw new ArgumentException("--snapshot-id must be followed by a valid GUID.");
                    snapshotId = parsedId;
                    break;
                default: throw new ArgumentException($"Unknown option: {args[index]}");
            }
        }

        if (mode == MigrationMode.Rollback && snapshotId is null)
            throw new ArgumentException("Rollback requires --snapshot-id.");
        if (mode != MigrationMode.Rollback && snapshotId is not null)
            throw new ArgumentException("--snapshot-id is only valid with --rollback.");
        if (confirmCleanup && mode != MigrationMode.Cleanup)
            throw new ArgumentException("--confirm-cleanup is only valid with --cleanup.");
        return new MigrationCommand(mode, batchSize, retentionDays, snapshotId, confirmCleanup);
    }

    private static MigrationMode SelectMode(MigrationMode current, MigrationMode next, ref bool modeSpecified)
    {
        if (modeSpecified)
            throw new ArgumentException("Choose exactly one operation.");
        modeSpecified = true;
        return next;
    }

    private static int ReadPositiveInt(string[] args, ref int index, string option)
    {
        if (++index >= args.Length || !int.TryParse(args[index], out var value) || value < 1)
            throw new ArgumentException($"{option} must be followed by a positive integer.");
        return value;
    }
}
