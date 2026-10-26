# Plan: Multi-route capture for URL snapshots

Status: approved, implemented, and validated
Date: 2026-10-26

## Goal

When a user submits a public URL, the resulting snapshot should contain not only the submitted document and the resources loaded by that document, but also the navigable routes of the same site that are discoverable from it. In the example below, both `/a` and `/b` must be stored as documents of the same snapshot and must remain navigable during replay:

```text
https://my-cool-site.net/
├── https://my-cool-site.net/a
└── https://my-cool-site.net/b
```

The implementation must remain bounded, must not weaken the existing SSRF and replay isolation boundaries, and must never label a capture `COMPLETE` if a discovered in-scope route could not be stored.

This proposal was explicitly approved before implementation. The implementation is complete on the dedicated feature branch and follows the scope and decisions documented below.

## Current implementation

### Capture path

URL submissions are processed by `Archive.Worker` through `BrowserCaptureEngine`:

1. `UrlNormalizer` accepts and normalizes one HTTP(S) source URL.
2. `PublicNetworkGuard` verifies that every requested network target resolves only to public addresses.
3. `CapturePageAccess` opens the submitted page in Playwright and currently permits the main frame to remain only on the exact normalized source URL.
4. `CapturedResourceCollector` stores successful responses that the page actually requests while rendering. It treats the main document separately and does not request ordinary hyperlinks.
5. `BrowserCaptureEngine.StoreResourcesAsync` builds one URL-to-object map, derives `index.html`, stores loaded resources below `assets/`, writes `source.html`, `screenshot.png`, and `manifest.json`, and marks captured HTML resources as `nested-document`.

The decisive current limitation is in `CapturePageAccess`: a main-frame navigation away from the exact submitted URL is blocked and later reported as `Weiterleitungen werden nicht automatisch archiviert.` A normal link to `/a` is not a resource request until it is followed, so `/a` and `/b` are absent from the collector and from the rewrite map.

### Stored-content and replay model

The existing storage and delivery model already contains most of the primitives needed for several documents:

- `SnapshotManifest` has the resource roles `root-document`, `nested-document`, and `asset`.
- `SnapshotsController` serves arbitrary manifest-authorized paths below the snapshot content route.
- `SnapshotContentPolicy` permits root and nested documents only for document/iframe destinations and applies the isolated snapshot policy to both.
- `SnapshotDocumentNormalizer` rewrites captured `href`, `src`, `poster`, `action`, and `srcset` references and injects the route-neutral `<base>` and runtime shim.
- Relative URLs work for public snapshots and for capability-prefixed owner/admin previews because every document resolves paths relative to the current snapshot root.

This means the public API shape, database schema, snapshot detail UI, and moderation model do not need to change. The missing capability is capture orchestration plus a complete document URL map.

### Existing safety and limit behavior

The current worker has hard limits for total duration, stored bytes, resources, and dynamic scroll rounds. Resource request failures are recorded in `manifest.json` and result in `SnapshotQuality.Incomplete`; storage/resource/duration limit violations discard the capture. The implemented crawl uses these limits for the whole snapshot rather than resetting them for every page.

The working tree was clean during analysis. Before implementation, `origin` was fetched and local `master` was confirmed to match `origin/master` at `dddedc5`; the feature branch was then created from that revision.

## Wayback Machine comparison

The Internet Archive uses two related but distinct concepts:

- The ordinary Save Page Now capture stores one submitted page and its embeds such as images, CSS, and JavaScript. The older Help Center documentation still describes this single-page default and explicitly distinguishes it from a whole-site crawl.
- The current Save Page Now form exposes an optional **Save outlinks** setting.
- The SPN2 API documents `capture_outlinks=1`. Embeds remain resources of the main capture, while captured outlinks receive their own job IDs and statuses.
- SPN2 follows redirects, but limits them to three.
- Automatic outlink capture is capped at 100. The documented ranking favors PDFs, EPUBs, URLs containing “new” or “update”, and URLs on the same domain.
- The SPN2 documentation warns against enabling outlink capture without a concrete need. It does not promise a recursive, exhaustive whole-site crawl.

Sources:

- [Current Save Page Now form](https://web.archive.org/save)
- [Internet Archive Help: Save Pages in the Wayback Machine](https://archivesupport.zendesk.com/hc/en-us/articles/360001513491-Save-Pages-in-the-Wayback-Machine)
- [Internet Archive Help: Using the Wayback Machine](https://archivesupport.zendesk.com/hc/en-us/articles/360004651732-Using-The-Wayback-Machine)
- [Archived SPN2 Public API documentation](https://archive.org/details/spn-2-public-api-page-docs-2023-01-22)
- [Readable mirror of the SPN2 API documentation](https://gist.github.com/regstuff/82e690db2f1d91ba59f6681c1abad6cf)

The Help Center text and current form are not fully synchronized: the former says outlinks are not saved, while the latter now offers that option. The current form and SPN2 contract are therefore the relevant evidence for the optional outlink behavior; the Help Center remains useful for the distinction between a page capture and an entire-site crawl.

### Consequences for this project

The useful Wayback principles are to:

- distinguish loaded dependencies from navigable documents;
- follow redirects only within a small explicit bound;
- cap the number of linked documents;
- keep linked-document failures visible instead of claiming a fully successful capture;
- avoid treating “save outlinks” as an unlimited whole-domain crawler.

Unlike Wayback's URL-centric model, Archive has one immutable snapshot object with one manifest and one replay entry point. Therefore, linked routes should be nested documents inside the same snapshot rather than separate snapshot records or processing jobs.

## Implemented behavior

### Crawl scope

The implementation defines “all further routes” as all HTTP(S) documents that meet every condition below:

1. They are reachable recursively from rendered `<a href>` or `<area href>` elements, starting at the submitted document.
2. After URL resolution and normalization, they have the same effective origin (scheme, host, and port) as the final seed document.
3. They are reached with a normal `GET` navigation and are not `download` links.
4. They have not already been queued or captured after fragment removal and normalization.
5. The crawl remains within the configured linked-page limit and the existing duration, resource, and storage limits.

The effective origin is chosen after following the seed URL's validated redirect chain. This supports common `http` to `https` and apex-host to `www` redirects without silently expanding the crawl to unrelated origins.

The crawler does not:

- follow external origins or subdomains;
- submit forms;
- execute `javascript:`, `mailto:`, `tel:`, `data:`, or other non-HTTP links;
- guess orphan routes, inspect server source code, or enumerate paths;
- treat API/resource URLs as pages merely because they share the origin;
- bypass authentication, challenge handling, network restrictions, or the project's existing crawler policy.

Query strings remain part of route identity because they may select materially different documents. Fragments do not create another capture, but a rewritten link must preserve its original fragment. This prevents `/a#one` and `/a#two` from consuming two crawl slots while keeping in-page navigation correct.

### Breadth-first, serial traversal

Use a deterministic breadth-first queue:

1. Capture the final seed document.
2. Extract normalized in-scope links from the post-execution DOM, after the existing lazy-load and scrolling pass.
3. Queue unseen URLs in DOM order.
4. Capture each queued document serially in the same browser context so cookies and challenge clearance remain available.
5. Discover further routes from every successful document until the queue is empty or a limit is reached.

Serial capture avoids hammering the target host, preserves deterministic ordering, and avoids resource-budget races. A single browser context is retained, but every document capture uses a fresh page so late responses from one route cannot contaminate another route's main-document state.

### Redirect policy

Redirect handling is changed only for main-document navigations:

- Validate every hop with `PublicNetworkGuard` before following it.
- Permit at most three hops, matching the documented SPN2 boundary.
- Permit the seed to establish its effective origin from its final public HTTP(S) URL.
- Require linked routes to finish on that effective origin. A linked route that redirects outside scope is not crawled further and is recorded as missing.
- Keep the existing Cloudflare challenge continuation rules; challenge URLs do not become crawl routes or manifest resources.
- Continue to reject unexpected document replacement after a route has reached its validated stable state.

Asset redirects remain routed one hop at a time through the existing request boundary and continue to require public-network validation.

### Linked-page limit

Add `Archive:Browser:MaxLinkedPages`, defaulting to `100`, for additional documents beyond the root. This mirrors Wayback's documented 100-outlink ceiling while making the project behavior configurable.

If more eligible routes are discovered:

- do not enqueue routes beyond the limit;
- record the omitted normalized URLs as missing;
- store the bounded capture as `INCOMPLETE` rather than `COMPLETE`.

The existing hard duration, resource, and storage limits remain authoritative. They apply to the complete crawl and retain their current terminal/discard semantics. A site cannot obtain a fresh budget for every route.

### Completion semantics

For URL submissions:

- `COMPLETE`: the seed succeeded, every discovered in-scope route within the queue was captured, and no captured dependency is missing.
- `INCOMPLETE`: the root is replayable, but at least one discovered in-scope linked route or dependency failed, redirected outside scope, returned an unsupported/non-success result, or was omitted by `MaxLinkedPages`.
- `FAILED`: the seed page itself cannot be accessed or validated, preserving the current behavior.
- `DISCARDED`: a hard duration, resource, or storage limit is exceeded, preserving the current behavior.

External links and intentionally unsupported schemes are out of scope and do not lower quality. Missing URLs must be de-duplicated across the crawl. The manifest continues to retain at most the first 100 missing URL strings while `missingUrlCount` records the full count.

## Storage and rewrite design

### Object layout

The URL-capture generation uses:

```text
snapshots/{snapshotId}/
├── index.html                         final seed document
├── pages/{sha256(canonical-url)}.html linked same-origin documents
├── assets/{sha256(canonical-url)}     scripts, styles, images, data, frames, and media
├── source.html                        immutable final seed response
├── screenshot.png                     seed viewport screenshot
└── manifest.json
```

Only the root screenshot is retained. Capturing a screenshot for every route is not required for replay and would consume significant storage without a corresponding UI or API contract.

### One global URL map

Build the final rewrite map only after crawling finishes:

- final root URL and its permitted redirect aliases -> `index.html`;
- every successfully captured linked document and its permitted redirect aliases -> `pages/{hash}.html`;
- every other successfully captured resource -> `assets/{hash}`.

Document mappings take precedence if a URL was observed both as an iframe/resource and as a deliberately crawled page. The object is stored once as a document, preventing duplicate manifest paths and ambiguous replay.

Every HTML document is normalized with the same global map. The existing `<base>` calculation makes root-relative snapshot paths resolve correctly from both `index.html` and `pages/...html`. Links between nested pages and links back to the root therefore stay inside the same public or capability-prefixed snapshot path.

The document URL resolver must preserve a link's fragment after using the fragment-free absolute URL as the lookup key. Query strings remain exact. The same behavior must cover static HTML attributes, `srcset`, import maps, and the existing exact-resource runtime resolution where applicable.

### Manifest compatibility

No manifest-version change is required. Version 4 already represents multiple HTML resources through `SnapshotResource` and `nested-document`:

- `index.html` remains the single `root-document`.
- Every captured route below `pages/` is a `nested-document` with its canonical source URL and normalized-content hash.
- Dependencies remain `asset` entries.
- Failed or omitted route URLs are added to `missingUrls`/`missingUrlCount`.
- `Interaction.ContainsExecutableContent` becomes true if any stored document contains executable content; the first normalization diagnostic remains available through the existing field.

Keeping manifest version 4 is important because the repository currently requires an exact `SnapshotManifest.CurrentVersion` match. A version bump without a multi-version reader would make existing snapshots unavailable. The existing contract can express the new artifacts without that compatibility risk.

`Snapshot.ResourceCount` continues to mean manifest-authorized resources excluding the root document. It therefore includes both linked documents and their unique assets, which is consistent with the existing treatment of captured nested HTML.

## Planned code changes

### Worker capture orchestration

Affected components:

- `backend/Archive.Worker/Capture/Browser/BrowserCaptureEngine.cs`
- `backend/Archive.Worker/Capture/Browser/CapturedResourceCollector.cs`
- `backend/Archive.Worker/Capture/Browser/CapturedResource.cs`
- `backend/Archive.Worker/Capture/Browser/Access/CapturePageAccess.cs`
- focused new types below `backend/Archive.Worker/Capture/Browser/Crawl/`
- a focused browser-snapshot writer below `backend/Archive.Worker/Storage/`

Planned responsibilities:

1. Introduce a small crawl-session model for the queue, canonical URLs, redirect aliases, captured documents, unique resources, missing URLs, and the global budget.
2. Extract one-document capture from `BrowserCaptureEngine` so the root and linked pages use the same access verification, dynamic loading, and resource collection path.
3. Extract links from the rendered DOM before the page closes and feed them through a pure same-origin route policy.
4. Change `CapturedResourceCollector` to contribute to the session-wide unique resource set and byte/resource budget rather than starting a fresh allowance for each page.
5. Refactor `CapturePageAccess` just enough to support the bounded redirect policy while retaining challenge detection and document-version verification.
6. Move the growing multi-document persistence logic out of `BrowserCaptureEngine` into a focused writer. This is a task-required separation, not a general refactor.
7. Generate the global document/resource map, normalize every document, write objects, write the manifest last, and roll back all newly written objects on failure using the existing `SnapshotObjectWriter` behavior.

The capture remains serial and single-worker. No new queue table, child processing records, service, or external crawler dependency is planned.

### Shared normalization

Affected components:

- `backend/Archive.Core/Storage/SnapshotDocumentNormalizer.cs`
- `backend/Archive.Core/Storage/SnapshotResourcePath.cs`
- optionally one focused route-normalization helper in `backend/Archive.Core/Jobs/`

Planned changes:

- use the global page-and-asset map for every stored document;
- retain URL fragments on rewritten document links;
- keep the existing exact, escaped, fragment-free URL key behavior for matching;
- avoid changing unrelated JavaScript rewrite patterns or replay security policy.

### Configuration and operations

Affected components:

- `backend/Archive.Worker/Capture/Browser/ArchiveBrowserOptions.cs`
- `backend/Archive.Worker/appsettings.json`
- `backend/Archive.Worker/appsettings.Development.example.json` if an explicit example is useful
- `.env.example`
- `deploy/production/.env.example`
- `deploy/production/api.compose.yml` (worker environment only)
- `docs/configuration.md`

Add and validate `MaxLinkedPages` with a minimum of `0`. A value of `0` preserves single-page behavior. Expose it in production as `ARCHIVE_BROWSER_MAX_LINKED_PAGES`, defaulting to `100`.

The API's `ArchiveOptions`, upload import path, database schema, and public HTTP contracts remain unchanged.

### Documentation

Affected components:

- `README.md`
- `docs/architecture.md`
- `docs/interactive-snapshots.md`
- `docs/configuration.md`
- `docs/development.md`

Document the crawl definition, same-origin boundary, object layout, quality semantics, cap behavior, redirects, and the new manual validation path. Do not describe the feature as an exhaustive site backup.

## Tests and validation

### Automated tests

Add focused coverage for:

1. **Route normalization and scope**
   - relative `/a` and `b` URLs resolve against the page URL;
   - same-origin links are accepted;
   - external origins, subdomains, non-HTTP schemes, and downloads are skipped;
   - fragments de-duplicate capture identity but survive rewritten navigation;
   - query variants remain distinct;
   - equivalent normalized URLs are captured only once.

2. **Queue and limits**
   - breadth-first ordering is deterministic;
   - cycles do not recapture documents;
   - more than `MaxLinkedPages` produces omitted/missing routes and `INCOMPLETE`;
   - unique resources shared by multiple documents count once;
   - resource and byte budgets are global.

3. **Redirect policy**
   - a public seed redirect establishes the effective origin;
   - linked same-origin redirects are accepted;
   - private-network targets, out-of-scope final origins, redirect loops, and a fourth hop are rejected;
   - Cloudflare challenge continuations remain distinct from crawl redirects.

4. **Storage and manifest**
   - root is `index.html`/`root-document`;
   - `/a` and `/b` are stable `pages/...html`/`nested-document` entries;
   - link aliases and shared assets resolve to one object;
   - missing linked pages affect manifest and quality;
   - resource count excludes only the root document;
   - manifest version remains 4 and existing version-4 fixtures remain readable.

5. **Browser replay**
   - an isolated Playwright test loads the root snapshot, follows a rewritten link to a nested document, follows a link back to the root, and verifies the expected content;
   - linked-page assets and exact captured fetches load through the snapshot path;
   - an uncaptured/external navigation does not reach a sentinel server;
   - public and capability-prefixed path shapes both resolve nested documents correctly.

Existing resource URL, content-policy, and dedicated-worker replay tests must continue to pass.

### Project checks

After implementation:

```powershell
dotnet build backend/Archive.slnx -m:1
dotnet test backend/Archive.slnx -m:1
Set-Location frontend
bun run check
bun run lint
bun run build
Set-Location ..
git diff --check
git status --short
```

The frontend checks are regression checks; no generated API types or frontend source changes are expected.

### Manual browser validation

Use a controlled public test site containing:

- `/` with links to `/a`, `/b`, an external URL, and fragment/query variants;
- `/a` linking to `/b` and back to `/`;
- `/b` with a shared asset and a missing same-origin link;
- one permitted redirect and one out-of-scope redirect.

Validate in the real snapshot viewer that:

- route navigation remains inside the snapshot content URL;
- browser devtools show no live request to the source site during replay;
- nested documents render consistently on desktop and mobile viewport sizes;
- public, owner, and administrator previews behave consistently;
- the complete fixture reports `COMPLETE` and the intentionally broken fixture reports `INCOMPLETE` with the correct counts.

There is no new application route or standalone UI view, so the required HTML mockup rule is not triggered. No visual design change is planned.

## Compatibility and impact

### Expected impact

- URL captures take longer and store more bytes because linked documents are actively visited.
- The same worker processes one capture for longer, but serial traversal bounds target load and avoids new concurrency behavior.
- `resourceCount` rises because linked documents become real snapshot resources.
- Existing completed snapshots remain immutable and readable.
- File uploads (HTML, MHTML, WebArchive) retain their current behavior.

### Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Infinite calendars, faceted navigation, or tracking-query explosions | Exact normalized URL de-duplication, same-origin boundary, global time/resource/storage limits, and `MaxLinkedPages`. |
| SSRF through links or redirects | Run every request and redirect hop through `PublicNetworkGuard`; never trust link scope alone. |
| Excess load on the source | Serial breadth-first capture; no parallel linked-page requests; existing overall deadline. |
| Duplicate resources across pages | One session-wide URL map and unique resource store. |
| Redirect changes crawl ownership | Establish scope from the final seed only; linked routes must finish within it. |
| A partial crawl appears successful | Missing/omitted in-scope routes force `INCOMPLETE`; hard limit violations retain discard behavior. |
| Stored links escape during replay | Rewrite every captured document from the final global map; preserve restrictive CSP, manifest roles, opaque iframe sandbox, and connection allowlist. |
| Existing snapshots become unreadable | Keep manifest version 4 and do not alter repository version matching. |
| JavaScript-only routes are undiscoverable | Discover links from the rendered DOM; document the remaining limitation for routes with no anchor/area URL. |

## Explicit non-goals

- A full domain/subdomain crawler or Archive-It replacement.
- Sitemap enumeration or brute-force route discovery.
- Form submission, login automation, or authenticated source capture.
- New snapshot records per route.
- Per-route screenshots or a route inventory UI.
- A database migration or public API schema change.
- Changes to moderation, ownership, search, tags, or file-import behavior.
- General refactoring outside the capture, normalization, configuration, tests, and directly affected documentation.

## Approved implementation sequence

1. Fetch the remote, verify that local `master` can be synchronized without overwriting user changes, and create `codex/multi-route-snapshot-capture` from the current main branch.
2. Add route-policy and crawl-session tests first.
3. Implement bounded redirect handling and one-document capture.
4. Add breadth-first same-origin orchestration with the shared resource budget.
5. Add multi-document storage and global rewriting without changing manifest version.
6. Add replay tests and run backend checks.
7. Update configuration/deployment documentation and run frontend regression checks.
8. Validate multi-route replay in a real Playwright browser. No new UI route or view is introduced, so an HTML mockup and responsive UI review are not applicable.
9. Review the final diff for task-only changes and create logically separated semantic commits, for example:
   - `feat: capture linked snapshot routes`
   - `test: cover multi-route snapshot replay`
   - `docs: document linked route capture`

## Approval decisions embodied by this plan

Approval of this plan also approves these concrete scope choices:

- recursive breadth-first traversal of rendered links, not only one-hop outlinks;
- same effective origin only, excluding subdomains;
- at most 100 additional linked documents by default;
- one root screenshot and one root immutable source object;
- linked documents stored inside the same snapshot as `nested-document` resources;
- no API, database, or frontend contract change;
- partial recoverable captures are stored as `INCOMPLETE`, while existing hard-limit violations remain terminal.

These scope choices were accepted with the explicit implementation approval.
