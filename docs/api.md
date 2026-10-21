# API

The ASP.NET Core application generates the API contract from controllers and response models. In Development, use:

- OpenAPI JSON: `http://localhost:5200/openapi/v1.json`
- Scalar reference: `http://localhost:5200/scalar/v1`

These endpoints are not mapped outside Development. The repository does not keep a generated OpenAPI JSON file under version control.

## Endpoint families

| Prefix | Purpose | Access |
| --- | --- | --- |
| `/healthz` | Process health | Public |
| `/api/auth` | Provider discovery, session, login, logout | Public entry points; session-aware |
| `/api/snapshots` | Public search/playback, submission, processing status/events/cancellation | Mixed public and session-aware operations |
| `/api/users` | Current profile and profile images | Session for current user; public handle image route |
| `/api/users/snapshots` | Owner submission list, details, history, private resources, resubmission | Authenticated owner |
| `/api/admin/permissions` | Permission catalog | Authorized administrator |
| `/api/admin/users` | User list, role, and permission management | Authorized administrator |
| `/api/admin/snapshots` | Review queue, private resources, and moderation | Authorized reviewer |

Use the live OpenAPI document for complete request fields, enum values, validation ranges, response schemas, and operation-specific status codes.

## Snapshot submission

`POST /api/snapshots` has two operations selected by content type.

| Content type | Source | Required shape |
| --- | --- | --- |
| `application/json` | Public URL | URL plus snapshot metadata and tags |
| `multipart/form-data` | Offline document | `.html`, `.mhtml`, or `.webarchive` file plus metadata and tags |

Both return `202 Accepted` with the snapshot ID, moderation status, status URL, and eventual snapshot URL. Submissions may be anonymous. When the request includes a valid application session, the created record is attributed to that user and the user's `snapshots.publish_without_approval` permission determines whether moderation starts as `APPROVED` or `PENDING`.

The location in the `202` response identifies transient processing state, not proof that the snapshot is already stored or public.

### Processing status

`GET /api/snapshots/{id}/status` returns the current queue position, status, progress step, cancellation state, start time, and moderation status. Terminal processing records expire after the configured retention window. A stored snapshot can still produce a synthesized `COMPLETED` status after that transient row has been cleaned up.

Processing statuses are:

- `QUEUED`
- `RUNNING`
- `COMPLETED`
- `FAILED`
- `CANCELLED`
- `DISCARDED`
- `ABORTED`

Progress steps are `SOURCE_CHECK`, `LOADING_PAGE`, `LOADING_DYNAMIC_CONTENT`, `SAVING_SNAPSHOT`, and `FINISHED`.

### Event stream

`GET /api/snapshots/{id}/events` is a `text/event-stream` endpoint.

- An unnamed `data` event contains the same JSON representation as the status endpoint.
- The API emits a comment heartbeat when state has not changed.
- The stream closes after a terminal status.
- A missing record or unavailable processing store returns a normal HTTP error before streaming begins.
- After streaming begins, those failures are emitted as a named `error` event before closure.
- Responses disable caching and proxy buffering through `X-Accel-Buffering: no`.

The frontend falls back to status polling when EventSource cannot stay connected. A reverse proxy must stream the response instead of buffering it.

### Cancellation

`POST /api/snapshots/{id}/cancel` cancels queued work immediately or requests cancellation of running work. Capture is no longer cancellable after storage has completed or another terminal outcome has been recorded.

## Authentication and browser credentials

Authentication begins with a browser form POST to `/api/auth/login/{provider}`. The API challenges the external provider and returns to the configured frontend. Google currently uses `/api/auth/callback/google` as its callback path.

The local API session uses an HTTP-only cookie. Browser calls to session, profile, owner, and administrator endpoints must include credentials. The API's credentialed CORS policy permits exactly `Authentication:FrontendOrigin`, allows request headers and methods, and exposes `ETag`.

Public API routes use the public CORS policy. Cookie-authenticated write routes still enforce their stricter origin and authorization requirements.

`GET /api/auth/session` is the canonical frontend session check. Anonymous callers receive an authenticated flag of `false`; a stale or invalid authenticated identity returns `401`.

## Public and private snapshots

Public snapshot search and playback include only records that have stored content and moderation status `APPROVED`. Other states do not become visible merely because the caller has an unrelated session.

Owner and administrator routes perform their own authorization before returning private detail, screenshots, or archived content:

| Scope | Detail base |
| --- | --- |
| Public | `/api/snapshots/{id}` |
| Owner | `/api/users/snapshots/{id}` |
| Reviewer | `/api/admin/snapshots/{id}` |

Resource URLs are scoped to the selected base. Archived files themselves contain route-neutral relative references so the same stored object set works through each authorized route.

Content endpoints support byte ranges. The root archived document and its resources are served with a restrictive content security policy. Original uploaded source files are returned as `application/octet-stream` attachments.

Private screenshot and HTML navigation requests additionally require a trusted frontend or same-snapshot navigation source. Private responses use `Cache-Control: private, no-store`.

## Conditional writes

The API uses strong ETags to prevent one browser view from overwriting a newer moderation or access state.

The sequence is:

1. Read the owner snapshot, review snapshot, or user-access resource.
2. Preserve its `ETag` response header.
3. Send exactly that value in `If-Match` on the mutation.
4. Reload after a successful response or a precondition failure.

Conditional operations include:

- `POST /api/users/snapshots/{id}/resubmit`;
- `POST /api/admin/snapshots/{id}/review`;
- `PATCH /api/admin/users/{handle}/permissions`.

These operations also require `Origin` to equal the configured frontend origin. Common failures are:

| Status | Meaning |
| --- | --- |
| `400` | The validator is malformed, more than one value was sent, or the request itself is invalid. |
| `403` | Origin or permission requirements are not met. |
| `409` | The requested state transition is not currently allowed. |
| `412` | The resource changed after the client read it. |
| `428` | `If-Match` is missing. |

Owner resubmission accepts no request body. Moderation accepts an `APPROVED` or `REJECTED` decision and an optional reason.

## Errors

Errors use `application/problem+json`. Model validation responses include field errors. State and authorization errors may include a stable `code` extension such as `snapshot_changed`, `if_match_required`, or `last_administrator`.

Clients should branch on HTTP status and a documented `code`, not the English title or detail. Those strings are intended for diagnostics and user-facing fallback text.

## Type generation

`frontend/openapi-ts.config.ts` generates TypeScript types into `frontend/src/lib/api/`:

```powershell
Set-Location frontend
bun run generate:api
```

The Development API must be running on port 5200. The generator cleans the output directory before writing, converts enums to screaming snake case, and formats generated files with Prettier.

See [Development](development.md) for the full workflow and [Architecture](architecture.md) for processing and access boundaries.
