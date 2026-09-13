# Architecture

This document describes the current implementation of archive.org. It is intended both for operators and for future contributors who need to reproduce the system without relying on unpublished design notes.

## System context

archive.org accepts an explicit request to preserve either a public HTTP(S) URL or an uploaded HTML document. Submission and capture are separated: the API validates and records work, while a single active worker performs browser automation and publishes the resulting snapshot.

The system deliberately separates metadata from large binary objects:

- PostgreSQL contains durable workflow state and searchable descriptive metadata.
- S3-compatible object storage contains uploaded source files and immutable snapshot payloads.
- The API joins both stores at read time and exposes stable public URLs.

## Component boundaries

### Frontend

Location: `frontend/`

The SvelteKit application renders the public archive index. Server-side remote functions call the API through `INTERNAL_API_BASE_URL` when running in Docker and fall back to `PUBLIC_API_BASE_URL` for browser-visible links.

The current interface supports:

- full-text-like term matching across title, description, source URL, and tags;
- exact normalized source filtering;
- title and description substring filters;
- tag, source type, quality, and capture-time filters;
- paginated results;
- screenshot inspection and links to stored HTML.

The public interface currently treats archive submission as a separate workflow. The API endpoints already exist even where the final submission route is not yet exposed in the frontend.

### API

Location: `backend/Archive.Api/`

The ASP.NET Core API owns:

- validation and normalization of submissions;
- storage of temporary uploaded HTML;
- creation, cancellation, status lookup, and event streaming for archive jobs;
- snapshot search and metadata retrieval;
- streaming archived HTML, assets, screenshots, and range-capable content responses;
- database migration on startup when `Database:Initialize` is enabled.

The API does not capture web pages itself. It writes a queued job and returns immediately.

### Core

Location: `backend/Archive.Core/`

The shared project defines:

- Entity Framework Core models and PostgreSQL mappings;
- migrations and database schemas;
- job, progress, quality, and source-type enums;
- URL normalization and project grouping;
- capture limits and object-storage interfaces;
- shared dependency registration.

### Worker

Location: `backend/Archive.Worker/`

The worker owns all untrusted network access and browser automation. It:

- acquires a PostgreSQL advisory lock so only one worker is active;
- recovers queue state after restart;
- claims work transactionally with `FOR UPDATE SKIP LOCKED`;
- checks `robots.txt` and host-level crawl delays;
- blocks non-public network destinations;
- optionally signs requests with Cloudflare Web Bot Auth;
- detects and refuses to archive challenge pages;
- captures the DOM, resources, screenshot, and manifest;
- publishes database metadata only after object storage succeeds;
- removes unpublished objects after a failed publication attempt when safe.

### Cloudflare bot identity

Location: `cloudflare/archive-bot-identity/`

This optional Cloudflare Worker serves:

- the public bot identity page;
- `robots.txt` for the identity origin;
- a signed Web Bot Auth public-key directory.

Its Ed25519 private key is a deployment secret. The archive worker receives the corresponding private key separately through a read-only filesystem mount.

## Runtime topology

```text
Browser
   |
   v
SvelteKit frontend -----> ASP.NET Core API
                              |       |
                              |       +-----> S3-compatible object storage
                              v
                         PostgreSQL <----- .NET capture worker
                                                |
                                                +-----> public target sites
                                                |
                                                +-----> optional bot identity origin
```

The frontend never receives database or object-storage credentials. The API and worker receive them at runtime through .NET configuration. Only the worker is expected to browse arbitrary public targets.

## Database model

PostgreSQL uses separate logical schemas:

- `processing` contains `archive_jobs` and the job-tag rows used while work is queued or running.
- `archive` contains projects, snapshots, normalized tags, and snapshot-tag relationships.
- `types` contains PostgreSQL enum types mirrored by the .NET enums.

An archive job and its successful snapshot share the same GUID. This makes status-to-result resolution direct and prevents the worker from accidentally creating multiple public snapshots for one completed job.

### Projects

URL snapshots are grouped by a project key derived from normalized host, port, and path. Query strings remain part of the stored source URL but are not part of the project key. Uploaded HTML receives a job-specific `html:<guid>` project key.

This grouping represents repeated observations of a logical page location. It does not imply that every query variant is semantically identical.

### Lifecycle states

| State | Meaning |
| --- | --- |
| `QUEUED` | Accepted by the API and waiting for the worker |
| `RUNNING` | Claimed by the active worker |
| `COMPLETED` | Objects and metadata were published successfully |
| `FAILED` | Capture or access failed |
| `CANCELLED` | Cancelled by an API request |
| `DISCARDED` | Deliberately abandoned after a configured system limit |
| `ABORTED` | A previously running job was invalidated after worker restart |

Progress moves through source check, page loading, dynamic-content loading, snapshot storage, and finished. Progress percentages are operational estimates, not measurements of page completeness.

## Queue and concurrency model

PostgreSQL is both the metadata store and the durable queue.

1. The worker holds a session-scoped advisory lock for its entire lifetime.
2. Additional worker instances wait on the same lock as warm standbys.
3. The active worker monitors the lock connection every two seconds.
4. Loss of that database session cancels the active capture.
5. A claim transaction selects the oldest queued job with row locking and `SKIP LOCKED`.
6. Jobs that were already running in the current lease window may be resumed after an ambiguous claim commit.
7. Jobs left running by an earlier process are marked `ABORTED` during startup recovery.

This design favors simple, deterministic, single-capture operation. It does not currently provide horizontal capture throughput. Changing that invariant requires redesigning recovery, host rate limiting, and publication ownership together.

## Publication consistency

Capture objects are written before the public snapshot row is committed. The worker then opens a database transaction, locks the job, verifies that it is still running, creates the snapshot and tag relationships, and marks the job completed.

If capture succeeds but publication does not:

- the worker first checks whether a durable snapshot row exists;
- if no snapshot exists, it removes stored objects in reverse order;
- if the database result is ambiguous, it keeps the objects because an orphan is safer than deleting content referenced by a committed snapshot.

PostgreSQL and S3 do not participate in a distributed transaction. The cleanup policy minimizes inconsistency but cannot eliminate every orphan scenario. Operators should eventually add an object-reconciliation job before running the system at large scale.

## Object layout

```text
jobs/<job-id>/source.html

snapshots/<snapshot-id>/index.html
snapshots/<snapshot-id>/screenshot.png
snapshots/<snapshot-id>/manifest.json
snapshots/<snapshot-id>/assets/<sha256-of-original-resource-url>
```

Asset names are the SHA-256 digest of the original resource URL. This creates deterministic keys and deduplicates repeated responses inside one capture. It is not a digest of the object bytes and therefore is not a fixity checksum.

The manifest is descriptive metadata, not a signed preservation manifest. See [How archiving works](archiving-process.md) for the exact meaning of completeness and integrity.

## HTTP surface

| Method and path | Purpose |
| --- | --- |
| `POST /api/archive` with JSON | Queue a public URL |
| `POST /api/archive` with multipart form data | Queue an uploaded HTML document |
| `GET /api/archive/{id}` | Read job status and result metadata |
| `GET /api/archive/{id}/events` | Stream changed job state as server-sent events |
| `POST /api/archive/{id}/cancel` | Cancel queued or running work |
| `GET /api/snapshots` | Search and page through snapshots |
| `GET /api/snapshots/{id}` | Read one snapshot |
| `GET /api/snapshots/{id}/content/{path}` | Stream archived HTML or an asset |
| `GET /api/snapshots/{id}/screenshot` | Stream the capture screenshot |
| `GET /healthz` | Process health endpoint |

OpenAPI and Scalar are mapped only in the Development environment.

## Trust boundaries

The main trust boundaries are:

1. User input entering the API.
2. Uploaded HTML entering object storage and the capture browser.
3. Public target responses entering the worker.
4. Archived active content later being served to readers.
5. Credentials entering containers at runtime.

Current safeguards include input limits, source URL normalization, HTML-upload network isolation, public-IP checks, explicit redirect handling, WebSocket blocking, crawl directives, capture limits, and challenge rejection.

Application-level DNS checks are not a complete SSRF defense. Production infrastructure must also prevent private and link-local egress. Likewise, archived HTML is active, untrusted content; it should be served from an isolated origin or behind an equivalent restrictive browser policy before public deployment.

## Code map for future changes

| Concern | Primary implementation |
| --- | --- |
| Submission validation and job creation | `Archive.Api/Controllers/ArchiveController.cs` |
| Snapshot search and content serving | `Archive.Api/Controllers/SnapshotsController.cs` |
| URL normalization and project keys | `Archive.Core/Jobs/UrlNormalizer.cs` |
| Database mappings | `Archive.Core/Entities/` and `Archive.Core/Migrations/` |
| Worker queue and publication | `Archive.Worker/ArchiveWorkerService.cs` |
| Exclusive worker lease | `Archive.Worker/ArchiveWorkerLease.cs` |
| Browser capture orchestration | `Archive.Worker/Capture/BrowserCaptureEngine.cs` |
| Navigation and network policy | `Archive.Worker/Capture/CapturePageAccess.cs` |
| Resource collection | `Archive.Worker/Capture/CapturedResourceCollector.cs` |
| `robots.txt` and crawl delay | `Archive.Worker/Capture/CrawlPolicyService.cs` |
| SSRF guard | `Archive.Worker/Capture/PublicNetworkGuard.cs` |
| Web Bot Auth signing | `Archive.Worker/Capture/WebBotAuthSigner.cs` |
| Cloudflare challenge detection | `Archive.Worker/Capture/CloudflareChallengeDetector.cs` |
| S3 implementations | `Archive.Api/Storage/` and `Archive.Worker/Storage/` |
| Public archive interface | `frontend/src/routes/(site)/` |

Paths in this table are relative to `backend/` unless they begin with `frontend/`.

## Deliberate current limitations

- One active capture worker at a time.
- No WARC serialization.
- No content-derived checksums, Merkle tree, timestamp authority, or signed manifest.
- No automatic recapture schedule or retention policy.
- No authenticated submission or administrative API.
- No distributed object/database transaction.
- No service-worker capture and no WebSocket capture.
- Same-page main-frame redirects are rejected except narrowly recognized challenge transitions.
- The screenshot records the configured viewport, not a stitched full-page image.
- “Complete” means no monitored resource failure was left unresolved; it does not prove semantic or visual completeness.

These limits are part of the present preservation model and should be changed explicitly rather than accidentally.
