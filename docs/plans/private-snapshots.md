# Plan: Owner-controlled private snapshots

**Status:** Approved by the user on 2026-10-28; implementation may begin

**Prepared:** 2026-10-28

**Scope:** Creation-time and later owner privacy, complete public invisibility, authorized replay, audit history, and existing-view UI controls

## 1. Objective and product decisions

Fully registered, signed-in users can create snapshots as private and change the privacy of snapshots attributed to their account later.

The proposed product rules are:

- privacy is a separate persisted dimension from moderation;
- `APPROVED` means the content passed moderation; it does not override owner privacy;
- a snapshot is public only when it is stored, approved, and not private;
- private snapshots are absent from public search results, counts, capture distributions, recent-capture data, metadata, public history, screenshots, and public content paths;
- owners and users whose effective permissions grant both `admin.access` and `admin.snapshots.access` retain access through existing private detail, screenshot, history, and capability-based content paths;
- only a fully registered owner can request privacy at creation time or change it later;
- existing snapshots default to public (`is_private = false`) so the migration does not silently hide current records;
- owner privacy changes are append-only audit events and participate in the strong ETag;
- changing privacy does not change moderation status or recapture content;
- privileged readers can see privacy state but do not receive an owner-privacy mutation control, even when their role grants administrative snapshot access.

The final rule keeps the requested authority with “their snapshots”. Read access is evaluated by the existing permission system, including role defaults and personal `ALLOW`/`DENY` overrides; mutation remains owner-only. A later administrator override would require a separately approved policy change.

## 2. Current architecture and constraints

### 2.1 Moderation currently doubles as publication state

Public repositories currently use:

```text
ModerationStatus == APPROVED && Quality != null
```

This predicate appears in search, public detail, screenshot, history, and content reads. Private owner/admin routes intentionally bypass approval after authorization and issue a short-lived capability URL for the credential-free content origin.

Privacy must not add a new moderation status. Doing so would conflate review and publication, complicate review transitions, and prevent an approved private snapshot from becoming public without another moderation decision.

### 2.2 Registration and ownership

Snapshot creation is currently available to anonymous visitors. An authenticated user becomes the snapshot owner only when `RegistrationCompletedAt` is set; incomplete accounts follow the anonymous, unattributed path.

A private snapshot therefore cannot be accepted unless the creation transaction resolves a fully registered creator. Client-side hiding of the control is not sufficient; the repository must enforce the same rule.

Owner edit routes already require the registered-user authorization policy, scope records by `CreatedByUserId`, require the configured frontend `Origin`, and require one strong `If-Match`. Those boundaries should be reused.

### 2.3 Existing private playback

Private moderation previews already use:

- authorized owner/admin metadata routes;
- a short-lived, subject-bound content capability in the path;
- a dedicated credential-free origin;
- resource-by-resource authorization revalidation.

This mechanism can serve owner-private snapshots without adding cookies to the snapshot origin. Public paths must reject private snapshots even when approved.

The existing authorized-content repository revalidates private access only when moderation is not approved. That condition must be tightened: every capability request should revalidate the subject, including an approved owner-private snapshot and an approved snapshot whose ownership or user access changed after the token was issued.

### 2.4 Processing endpoints

`/api/snapshots/{id}/status`, `/events`, and `/cancel` currently operate through the public creation workflow. A private creation would leak existence and lifecycle state if those routes remained accessible to anyone who learned the UUID.

Private status, event, and cancel access must therefore require the registered owner (with existing admin observation remaining available through administration detail routes). Frontend fetches and `EventSource` must include credentials for private submissions.

## 3. Data model and migration

Add a non-nullable Boolean to `Snapshot`:

```csharp
[Column("is_private")]
public bool IsPrivate { get; set; }
```

Generate an EF Core migration that:

- adds `archive.snapshots.is_private boolean NOT NULL DEFAULT false`;
- preserves all existing rows as public;
- adds a query-supporting index for the public visibility predicate and capture ordering if the generated query plan benefits from it;
- updates `DataContextModelSnapshot` through the established EF tooling.

A suitable index candidate is based on `is_private`, `moderation_status`, `quality`, `created_at`, and `id`. The exact PostgreSQL index should be confirmed against the generated search SQL rather than adding redundant indexes blindly.

Propagate `IsPrivate` through:

- `SnapshotDetails`;
- owner/admin submission summaries and detail projections;
- `SnapshotResponse` for private and public representations;
- `SnapshotSubmissionSummaryResponse`;
- creation response so the submitting UI can describe the accepted state.

Public responses will always contain `isPrivate: false` because private records never cross the public metadata boundary. Keeping the field in the shared response lets owner/admin UIs use the same snapshot representation without guessing from the route.

## 4. Visibility and authorization rules

### 4.1 Central public predicate

Introduce one composable repository expression or clearly named query helper for the public snapshot boundary:

```text
Quality != null
AND ModerationStatus == APPROVED
AND IsPrivate == false
```

Use it consistently for:

- text/filter search results;
- total public register count;
- year distribution;
- recent capture times;
- public `GET /api/snapshots/{id}`;
- public history;
- public screenshot;
- ordinary snapshot-origin content paths.

All public misses return `404`; no response should reveal whether a matching private record exists.

### 4.2 Access matrix

| Snapshot state | Anonymous/public route | Registered owner | Permission-authorized reader |
| --- | --- | --- | --- |
| Pending, public intent | Not public | Private submission access | Private administration access |
| Approved, public | Public access | Public or private detail access | Public or private detail access |
| Pending, private | `404` | Private submission/capability access | Private administration/capability access |
| Approved, private | `404` | Private submission/capability access | Private administration/capability access |
| Rejected/resubmitted, any privacy | `404` | Private submission/capability access when stored | Private administration/capability access |

Changing private to public exposes the snapshot immediately only if it is already stored and approved. Otherwise it becomes eligible for publication after the existing capture/moderation requirements are satisfied.

Changing public to private invalidates ordinary public metadata, history, screenshot, and content URLs immediately after the database transaction commits. Existing short-lived capability paths continue only when their bound subject still has owner/admin access.

### 4.3 Authorized content revalidation

Change `OpenAuthorizedContentAsync` so every capability request validates:

- the capability subject is a current, fully registered user with a supported role;
- the subject is still the snapshot owner, or `PermissionEvaluator` still allows both `admin.access` and `admin.snapshots.access` through role defaults or personal overrides;
- the snapshot still exists and has stored content;
- the requested resource remains authorized by the active manifest.

The public content method separately requires the central public predicate. This keeps privacy, moderation, ownership changes, permission revocation, and token expiry fail-closed.

### 4.4 Processing access

Add a scoped processing read/cancel boundary rather than exposing `IsPrivate` through an unguarded status record:

- public-intent processing retains the existing shareable progress behavior;
- private processing requires the current registered owner;
- unauthorized private requests return `404`;
- private SSE uses credentialed CORS and `EventSource(..., { withCredentials: true })`;
- private polling and cancellation use `credentials: "include"`;
- the worker's internal processing APIs remain unchanged.

The access decision should be made in the repository/service using current database state, not a creation-response claim held by the client.

## 5. Creation contract

Add `isPrivate` to both creation requests:

- JSON URL capture request;
- multipart file import request.

Omission defaults to `false` for backward compatibility. `SnapshotCreationRequest` carries the normalized Boolean into the creation transaction.

During creation:

1. lock/read the requested creator as today;
2. determine whether registration is complete;
3. if `isPrivate` is true and no fully registered creator exists, reject with a dedicated result mapped to `403` and a stable problem code such as `private_snapshot_requires_registration`;
4. set `CreatedByUserId` and `IsPrivate` atomically with the snapshot, history, tags, and processing row;
5. preserve the existing approval-bypass permission behavior independently of privacy;
6. return `isPrivate` in the `202 Accepted` response.

Anonymous and incomplete-account submissions remain supported only as public-intent submissions.

## 6. Owner update and history

### 6.1 Separate owner update model

Keep `SnapshotUpdate` as the shared metadata model and add an owner-specific wrapper, for example:

```csharp
public sealed record SnapshotOwnerUpdate(SnapshotUpdate Snapshot, bool IsPrivate);
```

`UpdateOwnAsync` accepts this model. `SnapshotAdministrationUpdate` remains unchanged, which prevents an administrator edit from accidentally changing owner privacy through shared form data.

The owner update transaction will:

- use the existing actor and snapshot row locks;
- verify ownership and current ETag;
- update metadata/source as today;
- update `IsPrivate` when changed;
- leave moderation unchanged for a privacy-only change;
- include `IsPrivate` in the ETag hash so concurrent visibility changes produce `412`;
- commit the visibility value and history event atomically.

### 6.2 Audit event

Add `snapshot.visibility_updated@1` with a payload that records previous and current privacy values. Append it:

- after a post-creation owner change;
- optionally at creation only when the initial state is private, so the private audit trail explains why an approved record was never public.

Owner and administrator history projections include the event. Public history excludes it entirely, including after a snapshot later becomes public, so a public record does not disclose private intervals or ownership decisions.

Add an English history renderer such as:

- **Snapshot made private**
- **Snapshot made public**

Use a lock/unlock icon plus text and a neutral/register tone; do not encode privacy with color alone.

## 7. Frontend changes

### 7.1 Feature-specific visibility control

Create a small feature component, tentatively `SnapshotVisibilityField.svelte`, outside `components/ui`. It is archive-specific because it owns privacy copy and publication semantics.

Suggested English copy:

- label: **Keep this snapshot private**
- description: **Only you and authorized archive staff can open it. It won’t appear in the public register or search.**

Use a native checkbox with a visible label and description. The complete label area should be clickable, focus must remain visible, and the target should meet the 44-pixel guidance. A generic switch abstraction is unnecessary unless another independent feature needs it.

### 7.2 Creation page

On `/archive/new`:

- add `isPrivate: false` to `ArchiveDraft`;
- render the visibility control only when `account.session.user.isRegistrationComplete` is true;
- send the Boolean in JSON and multipart creation bodies;
- retain public as the default;
- reset it to false when starting another capture;
- map backend registration/privacy errors to the field or form notice;
- use the creation response to distinguish **Ready in your private archive** from public publication messaging;
- keep **View submission** available and use the existing authorized snapshot route for opening stored private content.

The existing two-section form can include visibility after tags without creating a third route or standalone page. The page introduction may use neutral wording such as “Preserve a web page” rather than promising every registered submission enters the public record.

### 7.3 Owner edit page

Extend `SnapshotEditDraft` with `isPrivate` and render `SnapshotVisibilityField` only for `admin === false`.

The owner request includes the complete desired privacy state. A privacy-only save uses the same ETag, origin check, validation notice, conflict recovery, and post-save navigation as metadata edits.

The administrator edit view shows a read-only privacy badge in the detail metadata but does not render the owner control.

### 7.4 Submission and administration views

Add visible `Private` or `Public` state alongside moderation/capture information in:

- owner submission register;
- owner detail;
- administrator snapshot inventory;
- administrator detail;
- snapshot metadata where an authorized private reader needs context.

Use a small archive-specific `SnapshotVisibilityStatus` component backed by the existing `Badge` primitive. It renders a lock icon with **Private** and a globe icon with **Public**, using the quiet neutral badge treatment rather than warning semantics. Privacy must remain distinguishable from moderation: **Private · Approved** is a valid state and should not be collapsed into a single ambiguous badge.

On the capture-progress view, render moderation and visibility inside one wrapping status group. The group owns its spacing; neither status may use negative positioning or overlap the other at narrow widths. Visibility is shown for both public and private captures so the accepted publication state is explicit throughout processing.

### 7.5 No new route or mockup

All controls and states fit existing creation, edit, detail, list, and viewer routes. No independent view is planned, so the project rule requiring a separate HTML mockup does not apply.

The UI work must follow the existing editorial system: flat sections and rules, Source Sans 3 interface text, existing semantic colors, visible focus, quiet badges, light/dark equivalence, and responsive behavior to 320 CSS pixels.

## 8. API and generated contract changes

Update endpoint descriptions to state the combined publication rule and privacy authorization behavior. Expected contract changes include:

- `CreateSnapshotUrlRequest.isPrivate`;
- `CreateSnapshotFileRequest.isPrivate`;
- `SnapshotCreationResponse.isPrivate`;
- `SnapshotResponse.isPrivate`;
- `SnapshotSubmissionSummaryResponse.isPrivate`;
- `UpdateOwnSnapshotRequest.isPrivate`.

Regenerate the committed TypeScript API artifacts with the established workflow while the Development API is running:

```powershell
Set-Location frontend
bun run generate:api
```

Do not hand-edit generated API types or add a generated OpenAPI JSON document to the repository.

## 9. Expected file impact

### Backend Core

- `Archive.Core/Entities/Snapshot.cs` and `Configurations/SnapshotConfiguration.cs`;
- generated migration and `DataContextModelSnapshot`;
- snapshot creation, detail, summary, update, and history models;
- snapshot mapper projections;
- public snapshot/search/history repositories;
- submission repository reads and writes;
- processing access methods used by status/events/cancel;
- visibility history event type and serialization/projection support.

### Backend API

- URL/file creation contracts and response;
- owner update contract;
- snapshot/submission response contracts and mappers;
- public snapshot controller descriptions and processing access handling;
- owner/admin controllers only where new fields or outcomes are mapped.

### Frontend

- generated API types/exports;
- `archive-submission.ts` and `/archive/new`;
- `snapshot-edit-draft.ts`, `SnapshotEditForm.svelte`, and the feature visibility field;
- owner/admin registers and detail components;
- history event registry and a focused visibility event renderer;
- existing snapshot viewer messaging only where private state needs to be identified.

### Documentation

- `README.md` capability summary;
- `docs/architecture.md` visibility model and access matrix;
- `docs/interactive-snapshots.md` public versus capability paths;
- `docs/development.md` verification scenarios;
- `docs/deployment.md` capability invalidation expectations.

No new permission, authentication provider, account field, capture format, storage layout, or route is planned. Existing `admin.access` plus `admin.snapshots.access` is the read authorization contract; it never grants privacy mutation.

## 10. Validation strategy

### 10.1 Persistence and repository coverage

Add focused automated coverage for:

- migration defaulting every existing snapshot to public;
- private creation requiring a fully registered owner;
- public creation remaining available to anonymous and incomplete accounts;
- privacy remaining independent of approval bypass and moderation transitions;
- owner-only privacy updates and foreign-owner `404` behavior;
- privacy-only updates preserving moderation and capture state;
- ETag changes and stale `412` behavior;
- visibility history append and public-history exclusion.

### 10.2 Public invisibility coverage

For an approved, stored private snapshot, verify it is absent from:

- search items and result total;
- global public index total;
- year distribution and recent capture timestamps;
- public metadata;
- public history;
- public screenshot;
- ordinary public content URLs;
- anonymous status/events/cancel access when the snapshot was created private.

All endpoints should return `404` rather than a privacy-specific disclosure.

### 10.3 Authorized access coverage

Verify:

- the registered owner can list, inspect, edit, view history/screenshot, monitor processing, cancel, and replay a private snapshot;
- another registered user receives `404`;
- a user effectively allowed both `admin.access` and `admin.snapshots.access` can inspect and replay through existing admin routes, including when access comes from a personal override;
- a privileged non-owner cannot change `isPrivate` through owner or administration updates;
- revoked registration, ownership, role, or permission invalidates an already issued content capability;
- expired, modified, mismatched, and public-path capability variants return `404`;
- making a public snapshot private invalidates public URLs immediately;
- making an approved stored snapshot public publishes it without another review;
- making a pending/rejected snapshot public does not bypass moderation.

### 10.4 Frontend and visual coverage

Check in a browser:

- creation control only for complete registered accounts;
- public default and reset behavior;
- owner edit, conflict, error, loading, and success flows;
- distinct privacy and moderation badges in lists/details;
- private progress wording and links;
- keyboard access, screen-reader labels, 44-pixel target, visible focus, 200% zoom, 320-pixel reflow, long titles/URLs, and light/dark themes;
- desktop and mobile layouts with no new overflow or spacing regression.

Run the standard checks:

```powershell
dotnet build backend/Archive.slnx
dotnet test backend/Archive.slnx -m:1

Set-Location frontend
bun run generate:api
bun run check
bun run lint
bun run build
bun run build:storybook
```

`build:storybook` is required because existing shared primitives are reused and must remain intact; no new shared primitive story is required for the archive-specific visibility field.

## 11. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Private records leak through a secondary public aggregate | Reuse one public predicate for items, totals, distribution, detail, history, screenshot, and content; test every surface |
| Approval accidentally overrides privacy | Persist separate `IsPrivate`; require both approval and public intent |
| Anonymous client requests privacy | Enforce completed registration in the creation transaction, not only in UI |
| Old public URL remains usable after privacy change | Recheck `IsPrivate` in public metadata/content on every request |
| Capability remains valid after access changes | Revalidate owner/registration/permissions for every authorized resource request |
| Privacy change races with edit or moderation | Include privacy in the strong ETag and update under the existing snapshot lock |
| Admin shared form changes owner privacy accidentally | Use an owner-specific update wrapper and keep admin request unchanged |
| Private processing state leaks by UUID | Add owner-scoped status/events/cancel access and credentialed frontend monitoring |
| Public history exposes prior private intervals | Exclude visibility events from the public projection |
| Existing records become hidden during migration | Non-null column default is `false`; verify migration data |

## 12. Version-control execution

Before implementation:

1. Fetch `origin` and verify the local main branch against the current remote main branch.
2. Fast-forward without overwriting local changes.
3. Create a dedicated branch from updated main, proposed name `codex/private-snapshots`.
4. Keep the work independent from Safari playback and video import branches.

Proposed logical commits:

- `feat: add private snapshot visibility model`
- `feat: add owner privacy controls`
- `docs: document private snapshots`

## 13. Approval record

The user approved this plan on 2026-10-28 and clarified that only the snapshot owner may change visibility. Users with the effective existing administration permissions may continue to inspect and replay private snapshots, but cannot mutate privacy. The separate privacy dimension, complete public `404` boundary, credentialed private processing access, and private-only audit event behavior remain approved.
