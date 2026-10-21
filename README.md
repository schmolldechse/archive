# Archive

Archive is a public register of preserved web pages. It captures public URLs or imports HTML, MHTML, and WebArchive files, stores their resources and provenance, and publishes approved snapshots as immutable records.

Capture and publication are separate. A worker can finish storing a snapshot while that snapshot remains private until moderation approves it.

## Capabilities

- Capture public pages with Playwright, including loaded resources and a viewport screenshot.
- Import HTML, MHTML, and WebArchive documents.
- Search approved snapshots by text, tags, source, quality, and capture date.
- Inspect source, capture time, quality, resource count, size, submitter, and lifecycle history.
- Follow queued and running captures through server-sent events with polling fallback.
- Review submissions and manage role-based access with per-user permission overrides.
- Respect crawl policy, public-network boundaries, capture limits, and optional Web Bot Auth signatures.

## Architecture

| Area | Technology | Role |
| --- | --- | --- |
| [`frontend/`](frontend/) | Svelte 5, SvelteKit, TypeScript, Bun | Public register, submissions, accounts, moderation, and administration. |
| [`backend/Archive.Api`](backend/Archive.Api/) | ASP.NET Core, .NET 10 | HTTP API, authentication, authorization, search, content delivery, and database initialization. |
| [`backend/Archive.Worker`](backend/Archive.Worker/) | .NET 10, Playwright | URL capture, offline import, resource storage, and processing recovery. |
| [`backend/Archive.Core`](backend/Archive.Core/) | EF Core, Npgsql, AWS S3 client | Shared domain model, repositories, PostgreSQL access, and S3-compatible storage. |
| PostgreSQL | PostgreSQL 18 in the deployment stack | Metadata, accounts, permissions, history, and processing state. |
| MinIO | S3-compatible object storage | Snapshot content, screenshots, manifests, and temporary uploads. |
| [`cloudflare/archive-bot-identity/`](cloudflare/archive-bot-identity/) | Cloudflare Workers | Bot identity page and Web Bot Auth key directory. |

See [Architecture](docs/architecture.md) for component boundaries, capture flows, persistence, and access controls.

## Repository layout

```text
backend/                         .NET solution: API, worker, and shared core
cloudflare/archive-bot-identity Bot identity and signed key-directory worker
corporate-design/                Source and exported brand assets
deploy/production/               Production Docker Compose fragments
docs/                            Architecture, development, API, and operations guides
frontend/                        SvelteKit application and component stories
```

## Development

The application requires .NET 10, Bun 1, PostgreSQL, and S3-compatible object storage. The capture worker also requires Playwright Chromium.

Start with [Development](docs/development.md). It covers configuration, process order, API type generation, EF Core migrations, Storybook, and the checks available in this repository.

## Documentation

- [Architecture](docs/architecture.md)
- [Development](docs/development.md)
- [Configuration](docs/configuration.md)
- [Deployment](docs/deployment.md)
- [Bot identity worker](cloudflare/archive-bot-identity/README.md)

The API publishes its OpenAPI document at runtime in Development. The repository does not keep a generated API specification under version control.

## Current validation coverage

The repository currently has no automated test projects or CI workflow. Available checks are the .NET build, Svelte type/static checks, Prettier, production frontend build, Storybook build, and the Cloudflare Worker dry run described in [Development](docs/development.md#validation).
