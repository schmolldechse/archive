# Development

This guide runs the API, capture worker, and frontend as separate local processes. It relies only on tracked configuration examples. The repository does not currently include a tracked local Compose stack.

## Prerequisites

- .NET SDK 10
- Bun 1
- PowerShell 7 for the generated Playwright installer
- PostgreSQL reachable from the API and worker
- S3-compatible object storage with an existing bucket
- Google OAuth client credentials

The deployment stack currently uses PostgreSQL 18 and MinIO. Other compatible service versions are not covered by the repository's validation.

The Google client ID and secret are required at API startup because `Authentication:Google` is validated on startup. Register this local callback URI with the provider:

```text
http://localhost:5200/api/auth/callback/google
```

Local snapshot content uses the same API process through the distinct origin `http://127.0.0.1:5200`. Keep the API and frontend on `localhost`; the host distinction prevents application cookies from being sent to archived content.

## Prepare configuration

Create ignored development settings from the tracked examples:

```powershell
Copy-Item backend/Archive.Api/appsettings.Development.example.json backend/Archive.Api/appsettings.Development.json
Copy-Item backend/Archive.Worker/appsettings.Development.example.json backend/Archive.Worker/appsettings.Development.json
Copy-Item frontend/.env.example frontend/.env
```

Update both backend files with the same PostgreSQL connection and object-storage endpoint, credentials, and bucket. Set the API's Google credentials. Keep `Database:Initialize` enabled for the API and disabled for the worker so one process owns schema migration.

The root [`.env.example`](../.env.example) collects values for local orchestration and includes some direct .NET environment keys. Direct `dotnet run` processes do not load `.env` files, and names such as `OBJECT_STORAGE_SERVICE_URL` do not automatically become `ObjectStorage:ServiceUrl`. Use the development appsettings files or export the exact double-underscore .NET keys into each process environment.

Before starting the application, confirm that:

- PostgreSQL accepts the configured connection;
- the object-storage bucket exists;
- the configured storage credentials can read, write, and delete objects in that bucket;
- ports 5200 and 5173 are available.

## Install dependencies

Restore the .NET solution and local EF Core tool:

```powershell
Set-Location backend
dotnet tool restore
dotnet restore Archive.slnx
Set-Location ..
```

Install frontend dependencies from the lockfile:

```powershell
Set-Location frontend
bun install --frozen-lockfile
Set-Location ..
```

Build the worker once, then install the Chromium version required by its Playwright package:

```powershell
dotnet build backend/Archive.Worker/Archive.Worker.csproj
pwsh backend/Archive.Worker/bin/Debug/net10.0/playwright.ps1 install chromium
```

The production worker image performs the browser installation during its Docker build; the command above is for a direct local worker process.

## Start the application

Start the components in separate terminals after PostgreSQL and object storage are ready.

### 1. API

```powershell
dotnet run --project backend/Archive.Api
```

The `http` launch profile listens on `http://localhost:5200`. With the example development configuration, the API applies pending EF Core migrations before accepting requests.

Check it before starting dependent processes:

```powershell
Invoke-RestMethod http://localhost:5200/healthz
```

### 2. Capture worker

```powershell
dotnet run --project backend/Archive.Worker
```

The worker acquires the PostgreSQL processing lease, recovers interrupted work, and then waits for queued submissions. It exposes no HTTP port.

### 3. Frontend

```powershell
Set-Location frontend
bun run dev
```

Vite serves the application at `http://localhost:5173`. `PUBLIC_API_BASE_URL` in `frontend/.env` must point to the browser-reachable API origin. `INTERNAL_API_BASE_URL` may remain unset for local development so server-side frontend requests use the same address.

## Development endpoints

| Resource | URL |
| --- | --- |
| Frontend | `http://localhost:5173` |
| API health | `http://localhost:5200/healthz` |
| Public snapshot content | `http://127.0.0.1:5200/api/snapshots/{id}/content/index.html` |
| OpenAPI JSON | `http://localhost:5200/openapi/v1.json` |
| Scalar API reference | `http://localhost:5200/scalar/v1` |

OpenAPI and Scalar are mapped only when the API environment is Development.

## Generate frontend API types

The frontend's generated TypeScript contract is derived from the running development API. Start the API on port 5200, then run:

```powershell
Set-Location frontend
bun run generate:api
```

`frontend/openapi-ts.config.ts` reads `http://localhost:5200/openapi/v1.json`, replaces `frontend/src/lib/api/`, and formats the result. Review generated changes together with the controller or contract change that caused them.

Do not add a generated OpenAPI JSON file to the repository. The ASP.NET endpoint is the source of truth.

## Database migrations

Migrations and the model snapshot belong to `backend/Archive.Core`. Create a migration from the backend directory with the API as the startup project:

```powershell
Set-Location backend
dotnet ef migrations add <MigrationName> --project Archive.Core --startup-project Archive.Api
```

Review both the generated migration and `DataContextModelSnapshot.cs`. Normal development startup applies migrations through the API when `Database:Initialize` is enabled. The worker should not race the API for schema initialization.

To remove the latest unapplied migration:

```powershell
Set-Location backend
dotnet ef migrations remove --project Archive.Core --startup-project Archive.Api
```

## Storybook

Reusable Svelte components have stories under `frontend/src/stories/`.

```powershell
Set-Location frontend
bun run storybook
```

Build the static Storybook output when shared component behavior or stories change:

```powershell
bun run build:storybook
```

The generated `frontend/storybook-static/` directory is ignored.

## Validation

Run checks from the directories shown below.

### Backend

```powershell
dotnet build backend/Archive.slnx -m:1
dotnet test backend/Archive.slnx -m:1
```

### Frontend

```powershell
Set-Location frontend
bun run check
bun run lint
bun run build
```

Run `bun run build:storybook` when reusable components or their stories change.

### Documentation and repository state

```powershell
git diff --check
git status --short
```

Verify changed Markdown links and commands manually. No Markdown-link or prose linter is configured.

Use the relevant manual path when changing snapshot normalization, URL selection, capability gating, or response policies:

- submit one URL and one affected upload format;
- observe queued, running, and terminal updates, including polling fallback if changed;
- open an approved snapshot through the public route;
- open a pending or rejected snapshot through owner and authorized-reviewer routes;
- exercise moderation or access writes with a fresh ETag;
- edit owner metadata without changing approval, then change a source URL and confirm `RESUBMITTED`;
- create a private snapshot as a fully registered owner, confirm anonymous status and public discovery return no record, then switch it public and private again with a fresh ETag;
- confirm a user with effective `admin.access` and `admin.snapshots.access` can read and replay the private snapshot but cannot change its owner-controlled visibility;
- edit the same snapshot as an administrator, including removing and changing attribution, without a moderation transition;
- compare public, owner, and administrator history projections and confirm attribution events expose public profiles but no internal IDs;
- confirm owner and administrator previews use short-lived capability URLs and execute captured scripts without exposing private snapshots anonymously;
- confirm public and access-controlled snapshots execute captured scripts and dedicated `blob:` or exact captured workers for an admitted client while external requests, forms, Shared/Service Workers, storage, and navigation remain blocked;
- submit a controlled site with recursively linked same-origin routes, follow the stored links in public and private playback, and confirm browser networking never returns to the live source;
- verify fragments and query-bearing routes, a same-origin redirect, an external link, a missing route, and the configured linked-page limit produce the expected navigation and `COMPLETE`/`INCOMPLETE` quality;
- repeat with a missing, malformed, and below-minimum `Sec-CH-UA` hint and confirm the same URL remains static.

## Common startup failures

| Symptom | Check |
| --- | --- |
| API exits during option validation | Fill Google credentials, origins, connection string, and object-storage settings in the API development file. |
| Worker exits during option validation | Match its connection, storage, and browser settings to the running services. |
| API cannot migrate | Confirm the database exists and the configured PostgreSQL account may create schemas, tables, and enum types. |
| Submissions remain queued | Confirm the worker is running and can acquire the PostgreSQL advisory lease. |
| File/URL capture fails immediately | Confirm Chromium is installed for the worker and the source passes the public-network checks. |
| Stored content is missing | Confirm the bucket exists and API and worker use the same endpoint, bucket, and credentials. |
| Login redirects fail | Match the registered callback URI, `Authentication:PublicApiOrigin`, `Authentication:FrontendOrigin`, and frontend URL. |
| Frontend server loads fail in containers | Set `INTERNAL_API_BASE_URL` to the API service address while keeping `PUBLIC_API_BASE_URL` browser-reachable. |

See [Configuration](configuration.md) for every setting. The live OpenAPI document and Scalar reference remain the field-level HTTP contract.
