# archive.org

archive.org is an experimental public web archive for user-requested, timestamped snapshots of publicly reachable web pages and uploaded HTML, MHTML, or Webarchive documents.

> [!IMPORTANT]
> This repository is under active development. A snapshot is a best-effort representation of what the capture browser observed at a particular time. It is not yet a WARC record, a cryptographic proof of publication, or a guarantee that every part of a page was preserved.

## What the system preserves

For a URL capture, the worker stores:

- the final DOM serialized as HTML after dynamic content has been loaded;
- successfully loaded text, script, JSON, image, font, audio, and video responses;
- rewritten HTML and CSS references that point to the stored resources;
- a 1440 × 900 viewport screenshot;
- a JSON manifest containing the source URLs, media types, object keys, byte count, resource count, and completeness flag;
- searchable metadata in PostgreSQL, including title, description, tags, source URL, timestamps, quality, duration, and storage size.

Uploaded HTML, MHTML, and Webarchive files are imported without network access. Embedded resources, normalized HTML, a screenshot, a manifest, and the original file are stored; the temporary upload is deleted after successful publication.

The complete capture semantics, failure handling, and preservation limits are described in [How archiving works](docs/archiving-process.md).
Format-specific behavior and replay limits for uploads are described in [File imports](docs/file-imports.md).

## Architecture

| Component | Responsibility |
| --- | --- |
| SvelteKit frontend | Search, filter, inspect, and navigate the public snapshot index |
| ASP.NET Core API | Validate submissions, enqueue jobs, stream progress, search metadata, and serve stored content |
| .NET worker | Enforce crawl policy, operate Chromium through Playwright, collect resources, and publish snapshots |
| PostgreSQL | Durable job queue, snapshot metadata, projects, tags, lifecycle state, and the exclusive worker lease |
| S3-compatible storage | Uploaded sources, archived HTML, resources, screenshots, and manifests |
| Cloudflare identity worker | Optional public bot identity and Web Bot Auth key directory |

See [Architecture](docs/architecture.md) for component boundaries, persistence rules, concurrency behavior, and the code map.

## Quick start with Docker Compose

Requirements:

- Docker Desktop or Docker Engine with Compose;
- enough memory for Chromium and at least 1 GB of shared memory for the worker.

Create your local environment file:

```sh
cp .env.example .env
```

On PowerShell:

```powershell
Copy-Item .env.example .env
```

Replace at least `POSTGRES_PASSWORD`, `OBJECT_STORAGE_ACCESS_KEY`, and `OBJECT_STORAGE_SECRET_KEY` in `.env`. The file is ignored by Git.

Start the complete stack:

```sh
docker compose config --quiet
docker compose up --build
```

The local services are then available at:

- frontend: http://localhost:3000
- API: http://localhost:5200
- Scalar/OpenAPI in Development: http://localhost:5200/scalar
- MinIO console: http://localhost:9001

Web Bot Auth is disabled in `.env.example`. The empty `.secrets/` directory is mounted read-only so the stack can start without a key. To enable signed bot requests, place a private key at `.secrets/web-bot-auth-private-key.pem` and follow [Cloudflare and Web Bot Auth](docs/cloudflare-capture.md).

## Create an archive job

Submit a public URL:

```sh
curl -X POST http://localhost:5200/api/archive \
  -H "Content-Type: application/json" \
  -d '{"sourceUrl":"https://example.com/","title":"Example Domain","description":"Reference capture","tags":["example","reference"]}'
```

The response contains a job ID. Poll its state:

```sh
curl http://localhost:5200/api/archive/ARCHIVE_ID
```

Or subscribe to server-sent events until the job reaches a terminal state:

```text
GET /api/archive/ARCHIVE_ID/events
```

Submit an HTML, MHTML, or Webarchive file (use the matching extension):

```sh
curl -X POST http://localhost:5200/api/archive \
  -F "file=@page.html;type=text/html" \
  -F "title=Imported HTML" \
  -F "originalLink=https://example.com/" \
  -F "tags=imported"
```

List and search snapshots:

```sh
curl "http://localhost:5200/api/snapshots?text=example&page=1&pageSize=24"
```

## Local development without application containers

1. Start PostgreSQL and MinIO:

   ```sh
   docker compose up -d postgres minio minio-init
   ```

2. Create local application configuration if it does not already exist:

   ```powershell
   Copy-Item backend/Archive.Api/appsettings.Development.example.json backend/Archive.Api/appsettings.Development.json
   Copy-Item backend/Archive.Worker/appsettings.Development.example.json backend/Archive.Worker/appsettings.Development.json
   ```

3. Replace the placeholder credentials in both ignored development files with the values from `.env`.

4. Restore tools and apply database migrations:

   ```sh
   cd backend
   dotnet tool restore
   dotnet ef database update --project Archive.Core --startup-project Archive.Api
   ```

5. Build the worker once and install its matching Chromium build:

   ```powershell
   dotnet build Archive.Worker
   pwsh Archive.Worker/bin/Debug/net10.0/playwright.ps1 install chromium
   ```

6. Run the API and worker in separate terminals from their respective project directories:

   ```sh
   cd backend/Archive.Api
   dotnet run
   ```

   ```sh
   cd backend/Archive.Worker
   dotnet run
   ```

7. Start the frontend:

   ```sh
   cd frontend
   bun install --frozen-lockfile
   bun run dev
   ```

The API and worker launch profiles select the `Development` environment, which loads the ignored `appsettings.Development.json` files.

## Generated API client

With the API running in Development:

```sh
cd frontend
bun run generate:api
```

The generated files are written directly to `frontend/src/lib/api/`. Review changes after regeneration and do not edit generated types by hand.

## Configuration and deployment

[Configuration and operations](docs/configuration.md) documents every supported setting, configuration precedence, secret handling, image boundaries, database initialization, production safeguards, and backup responsibilities.

Important production facts:

- set both .NET environments to `Production`;
- inject database and object-storage credentials at runtime;
- never bake `.env`, `appsettings.Development.json`, `.dev.vars`, or private keys into an image;
- keep PostgreSQL and object storage private;
- restrict worker egress to public HTTP(S) destinations at the network boundary;
- place archived active HTML on an isolated origin or add an equivalent browser-security boundary;
- add authentication, quotas, and abuse controls before exposing archive submission publicly.

## Repository layout

```text
backend/                         ASP.NET Core API, shared data model, and capture worker
cloudflare/archive-bot-identity Optional public bot identity worker
corporate-design/                Logo sources and exported brand assets
docs/                            Public operational and architecture documentation
frontend/                        SvelteKit application and Storybook sources
.agents/                         Repository-local Codex skills
.codex/                          Repository-local Codex configuration
```

Internal specifications and implementation plans remain available in the local workspace but are excluded from Git through the root `.gitignore`. Generated `frontend/storybook-static/` output is also excluded; Storybook source stories remain publishable.

## Documentation

- [Architecture](docs/architecture.md)
- [How archiving works](docs/archiving-process.md)
- [Configuration and operations](docs/configuration.md)
- [Cloudflare and Web Bot Auth](docs/cloudflare-capture.md)
- [Editorial design manual](docs/design-manual-zeitregister.md)
