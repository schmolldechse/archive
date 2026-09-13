# Configuration and operations

archive.org uses configuration files for public defaults and environment variables or ignored development files for machine-specific values and secrets.

## Configuration precedence

ASP.NET Core and the .NET Generic Host apply their standard configuration order. In this project the effective priority is:

1. command-line arguments;
2. environment variables;
3. `appsettings.<Environment>.json`;
4. `appsettings.json`.

Nested .NET keys use double underscores in environment variables. For example, `ObjectStorage:SecretKey` becomes `ObjectStorage__SecretKey`.

Public `appsettings.json` files contain behavior limits and non-secret local endpoints only. Credentials belong in:

- ignored `appsettings.Development.json` files for direct local .NET runs;
- the ignored root `.env` for Docker Compose;
- a production secret manager or orchestrator-provided environment variables after deployment.

## Initial local setup

Copy the public templates:

```powershell
Copy-Item .env.example .env
Copy-Item backend/Archive.Api/appsettings.Development.example.json backend/Archive.Api/appsettings.Development.json
Copy-Item backend/Archive.Worker/appsettings.Development.example.json backend/Archive.Worker/appsettings.Development.json
```

Choose local database and object-storage credentials and place the same values in `.env` and both development JSON files.

Do not put real credentials into any `*.example` file.

## Docker Compose variables

### Application URLs

| Variable | Purpose | Local default |
| --- | --- | --- |
| `PUBLIC_API_BASE_URL` | API URL embedded in and exposed to the frontend | `http://localhost:5200` |
| `ARCHIVE_BACKEND_URL` | Local API URL used by frontend tooling | `http://localhost:5200` |
| `ARCHIVE_PUBLIC_BASE_URL` | Base URL emitted in snapshot API responses | `http://localhost:5200` |

The frontend container uses `http://api:8080` internally. Browser-visible links must use an address reachable by the user's browser.

### PostgreSQL

| Variable | Purpose |
| --- | --- |
| `POSTGRES_DATABASE` | Database name |
| `POSTGRES_USER` | Database role |
| `POSTGRES_PASSWORD` | Database password; required by Compose |

Compose constructs `ConnectionStrings__ArchiveDatabase` inside the API and worker containers. The database is exposed on port 5432 for local development; production deployments should remove that host publication unless an explicitly protected administrative path requires it.

### Object storage

| Variable | Purpose |
| --- | --- |
| `OBJECT_STORAGE_SERVICE_URL` | Host-visible S3-compatible endpoint |
| `OBJECT_STORAGE_INTERNAL_SERVICE_URL` | Container-network endpoint used by API and worker |
| `OBJECT_STORAGE_ACCESS_KEY` | Access key; required by Compose |
| `OBJECT_STORAGE_SECRET_KEY` | Secret key; required by Compose |
| `OBJECT_STORAGE_BUCKET` | Snapshot bucket |

The S3 clients accept either both explicit key values or neither. Omitting both lets the AWS SDK use its normal credential provider chain. Supplying only one is treated as a configuration error.

### Runtime environment

| Variable | Purpose |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | API environment |
| `DOTNET_ENVIRONMENT` | Worker environment |

The Compose example uses `Development` for local OpenAPI access and diagnostics. Use `Production` for deployed services. Development JSON files are excluded from Docker build contexts, so containers rely on injected environment values.

## Archive settings

### Capture limits

| .NET key | Meaning | Default |
| --- | --- | ---: |
| `Archive:MaxUploadBytes` | Maximum uploaded HTML size | 250,000,000 |
| `Archive:MaxDurationSeconds` | Capture deadline | 120 |
| `Archive:MaxStorageBytes` | Maximum stored snapshot bytes | 250,000,000 |
| `Archive:MaxResources` | Maximum retained resource responses | 500 |
| `Archive:MaxScrollRounds` | Maximum lazy-load scroll cycles | 24 |
| `Archive:QuietPeriodMilliseconds` | Pause between dynamic-content observations | 1,000 |

Limits intentionally fail or discard work rather than creating unbounded resource consumption.

### Browser

| Environment variable | .NET key | Purpose |
| --- | --- | --- |
| `ARCHIVE_BROWSER_HEADLESS` | `Archive:Browser:Headless` | Select headless or headed Chromium |
| `ARCHIVE_BROWSER_USER_AGENT` | `Archive:Browser:UserAgent` | Stable public bot User-Agent |
| `ARCHIVE_CHALLENGE_TIMEOUT_SECONDS` | `Archive:Browser:ChallengeTimeoutSeconds` | Maximum wait for automatic challenge clearance |
| `ARCHIVE_CHALLENGE_RELOAD_ATTEMPTS` | `Archive:Browser:ChallengeReloadAttempts` | Clean-capture retries after a challenge |

The worker container always starts through Xvfb so either browser mode can run without a desktop session.

### Crawl policy

| Environment variable | .NET key | Purpose |
| --- | --- | --- |
| `ARCHIVE_CRAWL_POLICY_ENABLED` | `Archive:CrawlPolicy:Enabled` | Enable `robots.txt` and archive-directive enforcement |
| `ARCHIVE_CRAWL_USER_AGENT_TOKEN` | `Archive:CrawlPolicy:UserAgentToken` | Token used for `robots.txt` group selection |
| `ARCHIVE_MINIMUM_HOST_DELAY_SECONDS` | `Archive:CrawlPolicy:MinimumHostDelaySeconds` | Minimum interval per origin |

Additional JSON settings:

- `Archive:CrawlPolicy:RequestTimeoutSeconds` defaults to 10.
- `Archive:CrawlPolicy:MaxRobotsBytes` defaults to 1,000,000.

Disabling crawl policy removes a deliberate site-owner control and should not be done for a public archival service without a documented governance decision.

### Web Bot Auth

| Environment variable | .NET key | Purpose |
| --- | --- | --- |
| `ARCHIVE_WEB_BOT_AUTH_ENABLED` | `Archive:WebBotAuth:Enabled` | Enable signed requests |
| `ARCHIVE_WEB_BOT_AUTH_SIGNATURE_AGENT` | `Archive:WebBotAuth:SignatureAgent` | HTTPS origin serving the public identity |
| `ARCHIVE_WEB_BOT_AUTH_PRIVATE_KEY_PATH` | `Archive:WebBotAuth:PrivateKeyPath` | Key path inside the worker |
| `ARCHIVE_WEB_BOT_AUTH_SIGNATURE_LIFETIME_SECONDS` | `Archive:WebBotAuth:SignatureLifetimeSeconds` | Validity of each request signature |
| `ARCHIVE_WEB_BOT_AUTH_KEY_DIRECTORY` | Docker mount source only | Host directory mounted at `/run/secrets` |

The expected local filename is:

```text
.secrets/web-bot-auth-private-key.pem
```

Compose mounts the directory read-only. The corresponding container path is:

```text
/run/secrets/web-bot-auth-private-key.pem
```

The key must be an Ed25519 PKCS#8 private key in the format accepted by NSec. Keep the host file readable only by the account that runs Docker.

See [Cloudflare and Web Bot Auth](cloudflare-capture.md) for identity publication and rotation.

## Database initialization

The API applies Entity Framework migrations on startup when `Database:Initialize` is true. The current API default is true. The worker default is false.

For controlled production rollouts, prefer an explicit migration step and set initialization to false in long-running application containers:

```text
Database__Initialize=false
```

The design-time `DataContextFactory` resolves its connection in this order:

1. `ConnectionStrings__ArchiveDatabase` environment variable;
2. `POSTGRES_CONNECTION` environment variable;
3. a local `Archive.Api/appsettings.Development.json` found relative to the working directory.

No fallback password is embedded in source code.

## Docker image boundaries

### Frontend image

The build stage receives development dependencies and source code. The final distroless Bun image contains only the Bun runtime and SvelteKit adapter output under `/app/build`, and runs with the unprivileged numeric identity `65532:65532`.

The frontend Docker context excludes dependencies, generated builds, Storybook output and configuration, stories, mockups, local environment files, and editor state.

### API image

The SDK exists only in the build stage. The final image is the .NET 10 Ubuntu Chiseled composite ASP.NET runtime with globalization data. It contains only published API output and runs as the built-in non-root .NET user.

Because the Chiseled image has no shell or HTTP utility, Compose calls the API executable's `--healthcheck` mode instead of adding a diagnostic package to the image.

### Worker image

The SDK exists only in the build stage. The final image starts from the .NET runtime rather than the SDK. During image construction it temporarily installs PowerShell to run the matching Microsoft.Playwright browser installer, installs the full Chromium browser plus its native dependencies, and removes the installer packages and package lists afterward. The separate legacy headless shell is omitted; headless captures use Playwright's `chromium` channel with the same full browser.

The final worker retains Chromium, Xvfb, Xauth, native browser libraries, the .NET runtime, and published worker output. These are required for headed browser operation without a desktop session.

The current worker runs the browser as the container's root user because sandboxed Playwright crawling requires an additional seccomp/user-namespace deployment profile. Before exposing the worker to high-risk targets, add that profile and a dedicated unprivileged user following Playwright's container guidance.

## Ignore-file responsibilities

Each ignore file is located where its owning tool actually evaluates it:

- the root `.gitignore` protects the single repository and applies recursively to backend, frontend, Cloudflare, documentation, and local tooling files;
- `backend/.dockerignore` filters the shared `backend/` Docker build context used by both the API and worker images;
- `frontend/.dockerignore` filters the separate `frontend/` Docker build context;
- `frontend/.prettierignore` belongs beside the frontend Prettier installation and configuration.

There is no root Docker build context, no repository-wide Prettier setup, and no independent Git repository inside `backend/` or `frontend/`. Additional ignore files in those locations would therefore be redundant or unused. A `.dockerignore` filters files sent to the builder; it is not copied into or read by the resulting container.

The root Git ignore rules deliberately exclude:

- local environment and secret files;
- `appsettings.Development.json`;
- dependencies, caches, and build outputs;
- generated `frontend/storybook-static/`;
- internal specifications and implementation plans under `docs/specification.md`, `docs/specifications/`, and `docs/superpowers/`.

Public documentation such as this file remains addable. Repository-local `.agents/` and `.codex/` configuration also remains addable.

Git ignore rules can always be overridden with `git add --force`. Review staged content before the first commit and before every release.

## Production checklist

Before deployment:

- use generated, unique database and object-storage credentials;
- store secrets in the deployment platform, not in files copied into images;
- set both .NET environments to `Production`;
- migrate the database in a controlled step;
- remove public PostgreSQL and MinIO ports;
- terminate TLS at a trusted reverse proxy;
- restrict API CORS to intended origins if the API is not meant to be universally readable;
- add authentication, authorization, quotas, request-size controls, and abuse prevention to submission endpoints;
- restrict worker egress at the network layer;
- isolate archived active content on a separate origin;
- define storage retention, backup, restore, and orphan-reconciliation procedures;
- monitor queue age, terminal failures, storage growth, worker lease health, and capture duration;
- pin or regularly review all container and package versions.

## Backup and restore

A recoverable archive requires both stores:

- PostgreSQL contains identifiers, metadata, object prefixes, tags, and job history.
- Object storage contains the preserved payloads.

Back up both on a coordinated schedule. Restoring only PostgreSQL produces broken object references; restoring only object storage produces undiscoverable objects. Periodic reconciliation should compare `archive.snapshots.content_prefix` values with `snapshots/<id>/` prefixes in the bucket.

The Web Bot Auth private key is operational identity material, not archive content. Back it up separately under stricter access control and document rotation independently.
