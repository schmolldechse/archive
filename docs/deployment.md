# Deployment

The production deployment is split into four Compose fragments under `deploy/production/`:

| File | Services |
| --- | --- |
| `database.compose.yml` | PostgreSQL |
| `minio.compose.yml` | MinIO server and one-shot bucket initialization |
| `api.compose.yml` | API and capture worker |
| `frontend.compose.yml` | SvelteKit frontend |

Together they form one Compose project on an external Docker network named `archive`. The repository does not include a reverse proxy; the deployment environment supplies TLS and routes the public frontend and API origins to the internal services.

## Host and origin contract

The host needs Docker Engine with Compose and enough storage for PostgreSQL, MinIO, build layers, and a temporary second copy during image replacement.

Provide three browser-visible HTTPS origins, for example:

```text
https://archive.example.com      frontend
https://api.archive.example.com  API
https://snapshots.example.net credential-free snapshot content
```

The reverse proxy must:

- route the frontend origin to container port 3000;
- route the API origin to container port 8080;
- route the snapshot origin to the same API container port without rewriting its host;
- preserve the original host and forwarding information;
- stream `/api/snapshots/{id}/events` without response buffering;
- allow long-lived event responses while the capture is active;
- avoid exposing PostgreSQL, MinIO, or its console unless the deployment explicitly needs operator access.

The snapshot origin must be outside `AUTHENTICATION_SESSION_COOKIE_DOMAIN`; a separate registrable domain is preferred. Do not attach authentication, CDN session cookies, or generic API routes to it. The application exposes only `/api/snapshots/{id}/content/{path}` on this host; all metadata, authentication, administration, and mutation routes remain available only through the API origin.

The API container sets `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so ASP.NET can interpret forwarded scheme and address information.

## Configure the stack

From the repository root, copy the production example:

```powershell
Copy-Item deploy/production/.env.example deploy/production/.env
```

Fill every value required by Compose interpolation. In particular, make the three origins consistent:

```dotenv
ORIGIN=https://archive.example.com
PUBLIC_API_BASE_URL=https://api.archive.example.com
ARCHIVE_PUBLIC_BASE_URL=https://api.archive.example.com
SNAPSHOT_PUBLIC_ORIGIN=https://snapshots.example.net
AUTHENTICATION_SESSION_COOKIE_DOMAIN=archive.example.com
```

Register this Google callback:

```text
https://api.archive.example.com/api/auth/callback/google
```

The complete variable mapping is in [Configuration](configuration.md#production-compose-inputs).

Create the external network once:

```powershell
docker network create archive
```

## Build and start

Run Compose from the repository root so the documented env-file path is unambiguous:

```powershell
docker compose --project-name archive --env-file deploy/production/.env -f deploy/production/database.compose.yml -f deploy/production/minio.compose.yml -f deploy/production/api.compose.yml -f deploy/production/frontend.compose.yml up -d --build
```

Startup readiness is encoded in the service dependencies:

1. PostgreSQL and MinIO become healthy.
2. `minio-init` creates the configured bucket if it does not exist.
3. The API starts, validates options, applies pending EF Core migrations, and becomes healthy at `/healthz`.
4. The worker starts after the API is healthy and acquires the PostgreSQL processing lease.
5. The frontend starts independently on the shared network and reaches the API at `http://api:8080` for server-side loads.

Only the API and frontend expose container ports for a reverse proxy. The Compose files do not publish host ports.

## Verify the deployment

Inspect service and health state with the same project definition:

```powershell
docker compose --project-name archive --env-file deploy/production/.env -f deploy/production/database.compose.yml -f deploy/production/minio.compose.yml -f deploy/production/api.compose.yml -f deploy/production/frontend.compose.yml ps
```

Then verify through the public origins:

```powershell
Invoke-RestMethod https://api.archive.example.com/healthz
Invoke-WebRequest https://archive.example.com/
```

Check the worker log for lease acquisition or processing errors:

```powershell
docker compose --project-name archive --env-file deploy/production/.env -f deploy/production/database.compose.yml -f deploy/production/minio.compose.yml -f deploy/production/api.compose.yml -f deploy/production/frontend.compose.yml logs --tail 100 worker
```

Complete deployment verification with one capture. Confirm that it leaves the queue, stores a screenshot and content, appears to its owner, and becomes public only under the expected moderation rule.

Before the first deployment of manifest-only snapshot delivery, follow [Snapshot manifest migration](snapshot-migration.md): pause the worker, run `Archive.SnapshotMigrator --dry-run --all`, migrate all candidates, and require a final result containing only `Current` snapshots. Deploying the new resolver while older generations remain would intentionally make those snapshots unavailable rather than bypassing manifest authorization.

For newly captured, migrated, public, and access-controlled snapshots, confirm that `contentUrl` uses the credential-free snapshot origin. Private URLs must contain a valid short-lived capability prefix and must return `404` after modification or expiration. Responses must vary on `Sec-CH-UA`; a supported Chromium must receive `Connection-Allowlist` plus `sandbox allow-scripts`, while a request without the required hint must receive the same stored bytes with `script-src 'none'`.

## Bootstrap the first administrator

External sign-in creates every new account with role `USER`. The application has no first-admin command or endpoint. Bootstrap exactly one completed profile in PostgreSQL, then use the admin UI for subsequent role and permission management.

1. Sign in through the deployed frontend and complete the target account's profile.
2. Open `psql` in the PostgreSQL container. Replace the placeholders with the deployment values from `.env`:

   ```powershell
   docker compose --project-name archive --env-file deploy/production/.env -f deploy/production/database.compose.yml exec postgres psql --username <POSTGRES_USER> --dbname <POSTGRES_DB>
   ```

3. In `psql`, begin a transaction and identify the exact completed account by its unique handle:

   ```sql
   BEGIN;

   SELECT id, handle, display_name, role, registration_completed_at
   FROM authentication.users
   WHERE handle = 'exact-handle';
   ```

4. Continue only when the query returns exactly the intended row with a non-null `registration_completed_at`. Assign the role and inspect it again:

   ```sql
   UPDATE authentication.users
   SET role = 'ADMIN', updated_at = clock_timestamp()
   WHERE handle = 'exact-handle'
     AND registration_completed_at IS NOT NULL;

   SELECT id, handle, role
   FROM authentication.users
   WHERE handle = 'exact-handle';
   ```

5. Confirm `UPDATE 1` and the `ADMIN` role, then commit. Otherwise roll back.

   ```sql
   COMMIT;
   -- or: ROLLBACK;
   ```

The `ADMIN` role grants the current default permission set, including portal, user, moderation, publication, role, and override management. The application prevents later changes from removing the last fully authorized administrator.

## Persistent state

| Volume or store | Contents | Recovery significance |
| --- | --- | --- |
| `database-data` | Snapshot metadata, history, processing state, users, roles, permissions | Required for all catalog and access state. |
| `minio-data` | Snapshot resources, manifests, screenshots, and temporary uploads | Required to render stored captures. |
| `api-key-ring` | ASP.NET data-protection keys | Preserves session-cookie readability across API replacement. |

Back up PostgreSQL and MinIO as one logical recovery point. A database record can reference an object prefix written shortly before its transaction committed, so unrelated backup times can restore metadata without the corresponding bytes or vice versa. The API key ring is separate from archive records but affects existing sessions after restore.

The MinIO image is built by `deploy/production/minio/Dockerfile`. It currently compiles `github.com/minio/minio@latest` and `github.com/minio/mc@latest`; the repository does not pin a MinIO release.

## Updates

Before an update, review EF Core migrations and any change to stored snapshot format. Database migrations run automatically when the new API starts.

A normal image update uses the same command as initial startup:

```powershell
docker compose --project-name archive --env-file deploy/production/.env -f deploy/production/database.compose.yml -f deploy/production/minio.compose.yml -f deploy/production/api.compose.yml -f deploy/production/frontend.compose.yml up -d --build
```

Verify API health before accepting new capture work, then watch the worker and submit a representative capture.

Code rollback is safe only when the previous version understands the current database schema and stored object format. EF Core does not automatically reverse migrations. If an update includes an incompatible migration, rollback requires the database/object backup or an explicit forward repair.

## Stop or remove containers

Stop services while retaining volumes:

```powershell
docker compose --project-name archive --env-file deploy/production/.env -f deploy/production/database.compose.yml -f deploy/production/minio.compose.yml -f deploy/production/api.compose.yml -f deploy/production/frontend.compose.yml stop
```

Remove containers and the project network attachments while retaining named volumes:

```powershell
docker compose --project-name archive --env-file deploy/production/.env -f deploy/production/database.compose.yml -f deploy/production/minio.compose.yml -f deploy/production/api.compose.yml -f deploy/production/frontend.compose.yml down
```

The external `archive` network remains after `down`. Named volumes remain unless explicitly removed.

## Web Bot Auth

When enabled, the worker reads `/run/secrets/web-bot-auth-private-key.pem` from the host directory selected by `ARCHIVE_WEB_BOT_AUTH_KEY_DIRECTORY`. `ARCHIVE_WEB_BOT_AUTH_SIGNATURE_AGENT` must identify the HTTPS origin deployed by the Cloudflare package, and its public key must match the mounted private key.

See [Bot identity worker](../cloudflare/archive-bot-identity/README.md) for key configuration and deployment verification.
