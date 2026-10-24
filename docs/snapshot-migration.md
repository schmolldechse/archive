# Snapshot manifest migration

`Archive.SnapshotMigrator` is the one-time operational tool for moving stored snapshots from older object layouts to the current manifest-backed snapshot contract. It uses the same PostgreSQL and object-storage configuration as the application and never contacts an archived page's live origin.

The migration concerns snapshot storage, not a separate playback representation. Every migrated snapshot keeps the same ID and public `contentUrl`. The database only switches its internal `ContentPrefix` after the new generation and its hashes have been validated.

## Safety model

The tool writes a new immutable generation below:

```text
snapshots/{snapshotId}/generations/manifest-v3/{migrationId}/
```

It records the original and migrated object sets in:

```text
snapshot-migrations/manifest-v3/{snapshotId}.json
```

The original generation remains untouched until cleanup. Activation and restore use conditional database updates, so a concurrently changed snapshot is not overwritten. A migrated legacy capture receives a current resource allowlist, roles, hashes, and provenance. Its interaction version remains unset because the older capture process cannot prove that executing its document is safe. This does not create a second delivery path: static and interactive snapshots are both authorized by the same manifest contract.

## Commands

Run the tool from `backend/` with the normal connection string and object-storage environment variables available:

```powershell
dotnet run --project Archive.SnapshotMigrator -- --dry-run --all
dotnet run --project Archive.SnapshotMigrator -- --dry-run --snapshot <snapshot-id>

dotnet run --project Archive.SnapshotMigrator -- --update --all
dotnet run --project Archive.SnapshotMigrator -- --update --snapshot <snapshot-id>

dotnet run --project Archive.SnapshotMigrator -- --restore --all
dotnet run --project Archive.SnapshotMigrator -- --restore --snapshot <snapshot-id>

dotnet run --project Archive.SnapshotMigrator -- --cleanup --all --confirm
dotnet run --project Archive.SnapshotMigrator -- --cleanup --snapshot <snapshot-id> --confirm
```

Add `--json` to `--dry-run`, `--update`, `--restore`, or `--cleanup` for machine-readable output.

For production, `snapshot-migrator.compose.yml` defines the one-off tool independently from the long-running API and worker services. It builds the same executable and attaches it to the existing internal database and object-storage network. Pass the CLI arguments after the service name:

```powershell
docker compose --project-name archive --env-file deploy/production/.env -f deploy/production/snapshot-migrator.compose.yml run --rm snapshot-migrator --dry-run --all
```

PostgreSQL and MinIO must already be running on the external `archive` network. Replace `--dry-run --all` with one of the other documented argument combinations when applying, restoring, or cleaning a migration. The Compose file does not define or start any long-running application service.

### Dry run

`--dry-run` reads and hashes stored objects without writing. It reports:

| State | Meaning |
| --- | --- |
| `Current` | The active generation has a valid current manifest. |
| `MigratableStatic` | A current manifest can be created, but script execution must remain disabled. |
| `Pending` | A prepared generation was not activated; cleanup is required before retrying. |
| `Restored` | The original generation is active and the migrated generation may be cleaned up. |
| `Blocked` | Required objects are absent, unsafe, or outside configured limits. |
| `Inconsistent` | Database state, manifest, hashes, or migration journal disagree. |
| `Missing` | The requested snapshot has no completed stored generation. |

The command exits nonzero for blocked, inconsistent, missing, or pending snapshots.

### Update

`--update` copies the complete stored generation, replaces its old manifest with a current manifest, validates every served resource, writes a prepared migration journal, and conditionally activates the new `ContentPrefix`. Already-current snapshots are skipped successfully, so the command is idempotent.

### Restore

`--restore` verifies that every original object still exists and switches the snapshot back to its previous content prefix and metadata. It does not delete either generation. Restore is available only while the migration journal and original generation remain present.

### Cleanup

`--cleanup` deletes only the inactive generation and then deletes the journal. It always requires `--confirm`. When `--all` is used, cleanup refuses to start unless every completed stored snapshot has a valid current manifest. If the migrated generation is active, cleanup permanently removes the original and disables restore. If the original was restored, cleanup removes the abandoned migrated generation.

## Production order

The manifest-only content resolver must not be deployed before stored snapshots have current manifests. Use a maintenance window:

1. Pause the worker so the stored snapshot set remains stable.
2. Publish the migrator from the release revision while the previous API remains available.
3. Run `--dry-run --all` and resolve every `Blocked`, `Inconsistent`, or `Pending` result.
4. Run `--update --all`.
5. Run `--dry-run --all --json`; every item must be `Current`.
6. Deploy the API and worker revision that requires manifest-backed delivery.
7. Retain migration journals and original generations for an observation period.
8. Run `--cleanup --all --confirm` only after restore is no longer required.

Do not run cleanup merely to save temporary storage. Keeping the original generation is the rollback mechanism.
