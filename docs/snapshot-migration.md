# Snapshot manifest migration

`Archive.SnapshotMigrator` is the one-time operational tool for moving stored snapshots from older object layouts to the current manifest-backed snapshot contract. It uses the same PostgreSQL and object-storage configuration as the application and never contacts an archived page's live origin.

The migration concerns snapshot storage, not a separate playback representation. Every migrated snapshot keeps the same ID and public `contentUrl`. The database only switches its internal `ContentPrefix` after the new generation and its hashes have been validated.

## Safety model

The tool writes a new immutable generation below:

```text
snapshots/{snapshotId}/generations/manifest-v4/{migrationId}/
```

It records the original and migrated object sets in:

```text
snapshot-migrations/manifest-v4/{snapshotId}.json
```

The original generation remains untouched until clean. Activation and restore use conditional database updates, so a concurrently changed snapshot is not overwritten. An older generation receives the current resource allowlist, roles, hashes, provenance, and interaction diagnostics. Its HTML documents and JavaScript resources pass through the same canonical normalization used by the worker, including snapshot-local runtime mapping and the defensive runtime shim. Every completed snapshot then uses the same manifest-backed content path and isolated JavaScript boundary.

## Commands

Run the tool from `backend/` with the normal connection string and object-storage environment variables available:

```powershell
dotnet run --project Archive.SnapshotMigrator -- --dry-run --all
dotnet run --project Archive.SnapshotMigrator -- --dry-run --snapshot <snapshot-id>

dotnet run --project Archive.SnapshotMigrator -- --migrate --all
dotnet run --project Archive.SnapshotMigrator -- --migrate --snapshot <snapshot-id>

dotnet run --project Archive.SnapshotMigrator -- --restore --all
dotnet run --project Archive.SnapshotMigrator -- --restore --snapshot <snapshot-id>

dotnet run --project Archive.SnapshotMigrator -- --clean --all --confirm
dotnet run --project Archive.SnapshotMigrator -- --clean --snapshot <snapshot-id> --confirm
```

Add `--json` to `--dry-run`, `--migrate`, `--restore`, or `--clean` for machine-readable output.

For production, `snapshot-migrator.compose.yml` defines the one-off tool independently from the long-running API and worker services. It builds the same executable and attaches it to the existing internal database and object-storage network. Pass the CLI arguments after the service name:

```powershell
docker compose --project-name archive --env-file deploy/production/.env -f deploy/production/snapshot-migrator.compose.yml run --rm snapshot-migrator --dry-run --all
```

PostgreSQL and MinIO must already be running on the external `archive` network. Replace `--dry-run --all` with one of the other documented argument combinations when applying, restoring, or cleaning a migration. The Compose file does not define or start any long-running application service.

### Dry run

`--dry-run` lists and validates older stored generations without writing. Current manifests and their authorized resources are additionally verified against their stored hashes. It reports:

| State | Meaning |
| --- | --- |
| `Current` | The active generation has a valid current manifest. |
| `Migratable` | A current manifest-backed generation can be created. |
| `Pending` | A prepared generation was not activated; clean is required before retrying. |
| `Restored` | The original generation is active and the migrated generation may be cleaned up. |
| `Blocked` | Required objects are absent, unsafe, or outside configured limits. |
| `Inconsistent` | Database state, manifest, hashes, or migration journal disagree. |
| `Missing` | The requested snapshot has no completed stored generation. |

The command exits nonzero for blocked, inconsistent, missing, or pending snapshots.

### Migrate

`--migrate` creates a complete immutable generation, normalizes legacy HTML and JavaScript with the worker's current snapshot rules, replaces the old manifest with a current manifest, validates every served resource, writes a prepared migration journal, and conditionally activates the new `ContentPrefix`. The immutable legacy `index.html` is retained as `source.legacy.html` when no separate source object exists. Already-current snapshots are skipped successfully, so the command is idempotent.

### Restore

`--restore` verifies that every original object still exists and switches the snapshot back to its previous content prefix and metadata. It does not delete either generation. Restore is available only while the migration journal and original generation remain present.

### Clean

`--clean` deletes only the inactive generation and then deletes the journal. It always requires `--confirm`. When `--all` is used, clean refuses to start unless every completed stored snapshot has a valid current manifest. If the migrated generation is active, clean permanently removes the original and disables restore. If the original was restored, clean removes the abandoned migrated generation.

## Production order

Use a maintenance window and deploy the compatible reader before changing stored generations. This keeps both the
original V1-V3 generation and the migrated V4 generation readable throughout migration and restore:

1. Pause the worker so the stored snapshot set remains stable.
2. Deploy the API revision with V1-V4 read support, prepare the migrator image from the same revision, and keep the new worker paused.
3. Run `--dry-run --all` and resolve every `Blocked`, `Inconsistent`, or `Pending` result.
4. Run `--migrate --all`.
5. Run `--dry-run --all --json`; every item must be `Current`.
6. Start the worker from the same release revision and verify one new capture plus the known older snapshots.
7. Retain migration journals and original generations for an observation period.
8. Run `--clean --all --confirm` only after restore is no longer required.

Do not run clean merely to save temporary storage. Keeping the original generation is the rollback mechanism.
