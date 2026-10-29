# Configuration

The API and worker use ASP.NET configuration. A key such as `Authentication:Google:ClientId` becomes `Authentication__Google__ClientId` when supplied as an environment variable. JSON settings, environment variables, and command-line configuration follow the normal .NET precedence rules.

SvelteKit reads browser-visible variables beginning with `PUBLIC_` and server-only variables through its private environment module. Production Compose maps uppercase deployment variables to the names consumed by each process.

## API

### Origins and authentication

| .NET key | Production Compose input | Default/example | Effect |
| --- | --- | --- | --- |
| `PublicBaseUrl` | `ARCHIVE_PUBLIC_BASE_URL` | API request origin when unset | Base used to generate public snapshot resource URLs. Must be an HTTP(S) URL without credentials, query, or fragment. |
| `Authentication:PublicApiOrigin` | `PUBLIC_API_BASE_URL` | `http://localhost:5200` | Browser-visible API origin and OAuth callback base. |
| `Authentication:FrontendOrigin` | `ORIGIN` | `http://localhost:5173` | Allowed credentialed CORS origin and post-login destination. |
| `Authentication:SessionCookieDomain` | `AUTHENTICATION_SESSION_COOKIE_DOMAIN` | unset for one host | Shared DNS domain when frontend and API use different hosts. |
| `Authentication:Google:ClientId` | `AUTHENTICATION_GOOGLE_CLIENT_ID` | none | Google OAuth client ID; required at API startup. |
| `Authentication:Google:ClientSecret` | `AUTHENTICATION_GOOGLE_CLIENT_SECRET` | none | Google OAuth client secret; required at API startup. |

### Snapshot content origin

| .NET key | Production Compose input | Default/example | Effect |
| --- | --- | --- | --- |
| `SnapshotContent:PublicOrigin` | `SNAPSHOT_PUBLIC_ORIGIN` | `http://127.0.0.1:5200` | Required credential-free origin used by public and authorized private snapshot content URLs. |
| `SnapshotContent:PrivateAccessTokenLifetimeMinutes` | `SNAPSHOT_PRIVATE_ACCESS_TOKEN_LIFETIME_MINUTES` | `60` | Lifetime of a private snapshot content capability issued after owner or administrator authorization. |

`SnapshotContent:PublicOrigin` must be a separate origin from the API and frontend. Outside Development it must use HTTPS, and its host must not be included by `Authentication:SessionCookieDomain`. The same API process serves this host, but host gating exposes only `/api/snapshots/{id}/content/{path}` and accepts only `GET`, `HEAD`, and `OPTIONS`. Public snapshots use the ordinary path; authorized private views receive a short-lived capability prefix on the same endpoint. Moderation state and browser identity do not determine whether captured JavaScript may execute.

Outside Development, the public API and frontend origins must use HTTPS. Both origins must use the same scheme. If their hosts differ, `SessionCookieDomain` must be a parent domain that includes both.

Register this callback with Google:

```text
{Authentication:PublicApiOrigin}/api/auth/callback/google
```

The application session cookie is HTTP-only, uses `SameSite=Lax`, and expires after 12 hours without sliding renewal. Production persists its data-protection keys in the API `key-ring` directory.

### Database, storage, and cleanup

| .NET key | Production Compose input | Default/example | Consumer and effect |
| --- | --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | Constructed from `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | local PostgreSQL on port 5432 | API and worker PostgreSQL connection. |
| `Database:Initialize` | fixed by host/Compose | API host default `true`; production worker `false` | Applies pending EF Core migrations during host startup. |
| `ObjectStorage:ServiceUrl` | fixed to `http://minio:9000` | `http://localhost:9000` | S3-compatible endpoint. |
| `ObjectStorage:AccessKey` | `OBJECT_STORAGE_ACCESS_KEY` | none | S3 access key; must be supplied together with the secret. |
| `ObjectStorage:SecretKey` | `OBJECT_STORAGE_SECRET_KEY` | none | S3 secret key; must be supplied together with the access key. |
| `ObjectStorage:Bucket` | `OBJECT_STORAGE_BUCKET` | `snapshots` | Bucket used by API and worker. |
| `Processing:TerminalRetentionSeconds` | `PROCESSING_TERMINAL_RETENTION_SECONDS` | `60` | Seconds a terminal processing record remains available; 5–3600. |
| `Processing:CleanupIntervalSeconds` | `PROCESSING_CLEANUP_INTERVAL_SECONDS` | `30` | API cleanup-loop interval; 1–3600 seconds. |
| `Processing:CleanupBatchSize` | `PROCESSING_CLEANUP_BATCH_SIZE` | `100` | Maximum expired rows removed per API cleanup pass; 1–1000. |

Only the API cleanup service uses cleanup interval and batch size. Both API and worker use terminal retention when they calculate transient processing expiry.

## Shared capture limits

These keys live under `Archive` and are consumed by submission validation or worker capture/import.

| .NET key | Default | Effect |
| --- | --- | --- |
| `Archive:MaxUploadBytes` | `250000000` | Maximum original upload bytes. The API permits an additional 1,000,000 bytes of multipart framing. |
| `Archive:MaxDurationSeconds` | `120` | Overall URL or import capture deadline. |
| `Archive:MaxStorageBytes` | `250000000` | Maximum bytes written for a stored snapshot. |
| `Archive:MaxResources` | `500` | Maximum captured or imported resource count. |
| `Archive:MaxScrollRounds` | `24` | Maximum scrolling passes used to reveal dynamic page content. |
| `Archive:QuietPeriodMilliseconds` | `1000` | Network/content quiet period used before capture completion. |
| `Archive:MaxImportDepth` | `32` | Maximum nested object depth while parsing imports. |
| `Archive:MaxImportObjects` | `100000` | Maximum object count while parsing imports. |

The checked-in appsettings files explicitly list the first six values. The import limits use the option-class defaults unless overridden.

## Worker

### Browser capture

| .NET key | Production Compose input | Default | Effect |
| --- | --- | --- | --- |
| `Archive:Browser:Headless` | `ARCHIVE_BROWSER_HEADLESS` | `true` | Runs Chromium without a visible window. The production container still provides a virtual display. |
| `Archive:Browser:UserAgent` | `ARCHIVE_BROWSER_USER_AGENT` | `ArchiveBot/1.0` | ASCII user agent sent by the browser. |
| `Archive:Browser:ChallengeTimeoutSeconds` | not mapped by Compose | `45` | Maximum challenge wait; 1–300 seconds. |
| `Archive:Browser:ChallengeReloadAttempts` | not mapped by Compose | `1` | Reload attempts for a detected challenge; 1–3. |
| `Archive:Browser:MaxLinkedPages` | `ARCHIVE_BROWSER_MAX_LINKED_PAGES` | `100` | Maximum additional same-origin HTML documents captured recursively for one URL snapshot. `0` preserves single-page capture behavior. |

## Frontend

| Variable | Visibility | Development value | Effect |
| --- | --- | --- | --- |
| `PUBLIC_API_BASE_URL` | Browser and server | `http://localhost:5200` | Browser-reachable API origin. Trailing slashes are removed. |
| `INTERNAL_API_BASE_URL` | Server only | unset | Optional API origin for SvelteKit server loads, normally `http://api:8080` in Compose. |
| `ORIGIN` | Server/runtime | `http://localhost:5173` implicitly in dev | Canonical SvelteKit origin; required by the production container. |
| `HOST` | Server/runtime | Vite default | Bind address; production sets `0.0.0.0`. |
| `PORT` | Server/runtime | `5173` for Vite | Bun-adapter listen port; production sets `3000`. |

Never set `INTERNAL_API_BASE_URL` to an address the browser must use. It is intentionally accessed only by server-side code.

## Production Compose inputs

`deploy/production/.env.example` is the input contract for the four Compose fragments.

| Variable | Used by |
| --- | --- |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | PostgreSQL and constructed API/worker connection strings |
| `OBJECT_STORAGE_ACCESS_KEY`, `OBJECT_STORAGE_SECRET_KEY`, `OBJECT_STORAGE_BUCKET` | MinIO initialization, API, and worker |
| `ORIGIN` | Frontend runtime and API frontend origin |
| `PUBLIC_API_BASE_URL` | Frontend browser URL and API authentication origin |
| `ARCHIVE_PUBLIC_BASE_URL` | Public snapshot URLs generated by the API |
| `SNAPSHOT_PUBLIC_ORIGIN`, `SNAPSHOT_PRIVATE_ACCESS_TOKEN_LIFETIME_MINUTES` | Isolated snapshot delivery and private capability lifetime |
| `AUTHENTICATION_GOOGLE_CLIENT_ID`, `AUTHENTICATION_GOOGLE_CLIENT_SECRET` | API Google provider |
| `AUTHENTICATION_SESSION_COOKIE_DOMAIN` | API session sharing across frontend/API hosts |
| `PROCESSING_TERMINAL_RETENTION_SECONDS` | API and worker |
| `PROCESSING_CLEANUP_INTERVAL_SECONDS`, `PROCESSING_CLEANUP_BATCH_SIZE` | API cleanup loop |
| `ARCHIVE_BROWSER_HEADLESS`, `ARCHIVE_BROWSER_USER_AGENT`, `ARCHIVE_BROWSER_MAX_LINKED_PAGES` | Worker browser and linked-page crawl |

## Values that must agree

- `Authentication:PublicApiOrigin`, frontend `PUBLIC_API_BASE_URL`, the external API route, and the Google callback origin must describe the same browser-visible API.
- `Authentication:FrontendOrigin`, frontend `ORIGIN`, and the external frontend route must describe the same origin.
- `SnapshotContent:PublicOrigin` and the external snapshot route must describe the same credential-free origin outside the session-cookie domain.
- When the two origins use sibling hosts, `Authentication:SessionCookieDomain` must include both.
- API and worker must use the same PostgreSQL database, object-storage endpoint, credentials, bucket, and terminal retention.
- The MinIO initializer must create the same bucket named by API and worker configuration.

See [Development](development.md) for local files and [Deployment](deployment.md) for the Compose mapping.
