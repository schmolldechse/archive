namespace Archive.SnapshotMigrator;

internal enum SnapshotMigratorAction
{
    DryRun,
    Migrate,
    Restore,
    Clean
}

internal sealed record SnapshotMigratorOptions(
    SnapshotMigratorAction Action,
    Guid? SnapshotId,
    bool All,
    bool Confirm,
    bool Json)
{
    public static string Help => """
        Usage:
          Archive.SnapshotMigrator --dry-run (--snapshot <id> | --all) [--json]
          Archive.SnapshotMigrator --migrate (--snapshot <id> | --all) [--json]
          Archive.SnapshotMigrator --restore (--snapshot <id> | --all) [--json]
          Archive.SnapshotMigrator --clean   (--snapshot <id> | --all) --confirm [--json]

        Actions:
          --dry-run  Validate snapshot manifests and report migration readiness without writing.
          --migrate  Create and activate a current manifest-backed snapshot generation.
          --restore  Reactivate the original generation retained by an earlier migration.
          --clean    Delete the inactive generation and migration journal. This disables restore.
        """;

    public static bool TryParse(string[] args, out SnapshotMigratorOptions? options, out string? error)
    {
        options = null;
        error = null;
        SnapshotMigratorAction? action = null;
        Guid? snapshotId = null;
        var all = false;
        var confirm = false;
        var json = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            SnapshotMigratorAction? parsedAction = argument switch
            {
                "--dry-run" => SnapshotMigratorAction.DryRun,
                "--migrate" => SnapshotMigratorAction.Migrate,
                "--restore" => SnapshotMigratorAction.Restore,
                "--clean" => SnapshotMigratorAction.Clean,
                _ => null
            };
            if (parsedAction is not null)
            {
                if (action is not null)
                {
                    error = "Specify exactly one action.";
                    return false;
                }
                action = parsedAction;
                continue;
            }

            switch (argument)
            {
                case "--snapshot":
                    if (snapshotId is not null)
                    {
                        error = "--snapshot may only be specified once.";
                        return false;
                    }
                    if (++index >= args.Length || !Guid.TryParse(args[index], out var parsedId))
                    {
                        error = "--snapshot requires a valid snapshot UUID.";
                        return false;
                    }
                    snapshotId = parsedId;
                    break;
                case "--all":
                    all = true;
                    break;
                case "--confirm":
                    confirm = true;
                    break;
                case "--json":
                    json = true;
                    break;
                case "--help" or "-h":
                    error = string.Empty;
                    return false;
                default:
                    error = $"Unknown argument: {argument}";
                    return false;
            }
        }

        if (action is null)
        {
            error = "An action is required.";
            return false;
        }
        if (all == (snapshotId is not null))
        {
            error = "Specify exactly one selector: --snapshot <id> or --all.";
            return false;
        }
        if (action == SnapshotMigratorAction.Clean && !confirm)
        {
            error = "--clean is irreversible and requires --confirm.";
            return false;
        }
        if (action != SnapshotMigratorAction.Clean && confirm)
        {
            error = "--confirm is only valid with --clean.";
            return false;
        }

        options = new(action.Value, snapshotId, all, confirm, json);
        return true;
    }
}
