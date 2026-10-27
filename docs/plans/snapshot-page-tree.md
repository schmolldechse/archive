# Plan: Snapshot page-tree contract and visualization

Status: implemented and validated
Date: 2026-10-27

## Goal

Expose the page hierarchy produced by multi-route URL capture through the public API and render it consistently in:

- `/account/submissions/{snapshotId}`;
- `/admin/snapshots/{snapshotId}`;
- a new **Pages** tab in the snapshot-information dialog on `/snapshots/{snapshotId}`.

The result should be visually distinctive and useful for understanding a capture, while remaining consistent with the archive's restrained editorial design, accessible without a pointer, responsive down to 320 CSS pixels, and bounded by the existing maximum of 100 linked pages plus the root page.

This is a follow-up to `docs/plans/multi-route-snapshot-capture.md`. That plan deliberately treated a route inventory UI and public API change as non-goals. Approval of this follow-up would supersede only those two non-goals; the capture scope, safety boundaries, replay model, and completion semantics remain unchanged.

Implementation was explicitly approved and completed on `codex/multi-route-snapshot-capture` so the additive contract remains part of the same draft pull request as the capture behavior it describes.

## Implementation record

- Manifest version 4 now optionally stores the validated flat first-discovery tree; existing manifests remain readable.
- Public, owner, and administrator page-tree subresources expose one generated response contract without adding tree data to snapshot lists.
- The three requested frontend surfaces share one archive-specific diagram/outline component built with LayerChart 2.9.1 and `d3-hierarchy` 3.1.2.
- A clean production comparison against the pre-feature branch state measured a 570,771-byte minified / 184,273-byte gzip increase across generated client JavaScript. The chart code remains route-split from unrelated application views.
- A fresh five-route HTTPBin snapshot verified breadth-first storage, the public contract, diagram and outline selection, dark and light themes, and navigation between stored routes.
- Automated validation covers capture structure, manifest validation, frontend normalization, Svelte type checking, production builds, and Storybook.

## Existing architecture

### Capture and storage

`BrowserCaptureEngine` now performs a serial breadth-first crawl of rendered, same-origin links. It retains:

- a root `CapturedDocument`;
- a breadth-first `documents` list;
- URL aliases for redirects and de-duplication;
- each document's rendered `DiscoveredLinks`;
- root and nested document resources in manifest version 4.

The current manifest is sufficient to authorize and replay every stored document, but it is not sufficient to reproduce the crawl hierarchy. Each `SnapshotResource` records its own source URL, stored path, content type, hash, and role. It does not record which captured document first discovered another document. URL path prefixes also cannot establish that relationship reliably.

Consequently, a tree inferred later from `resources` would be guesswork. The relationship must be recorded while the breadth-first queue still knows both the candidate URL and the page that discovered it.

### API

Public detail and list operations both use `SnapshotResponse`. Private owner and administrator detail operations wrap the same response in `SnapshotSubmissionDetailResponse`. Adding as many as 101 page nodes directly to `SnapshotResponse` would also add them to every search result unless the response became context-dependent.

The existing API already models large supplementary data as snapshot subresources:

- public, owner, and administrator history endpoints;
- public, owner, and administrator screenshot endpoints;
- manifest-authorized content paths.

A page-tree subresource fits this architecture and avoids object-store reads and large repeated payloads on snapshot lists.

### Frontend

The owner and administrator detail routes both render `SubmissionDetail.svelte`, so one integration covers both requested pages. The public/private snapshot viewer uses `SnapshotInformationDialog.svelte`, which already has the reusable accessible Tabs family and currently exposes **Info** and **History**.

The frontend is on Svelte 5.57, Tailwind CSS 4.3, and uses design tokens from `app.css`. It currently has no charting dependency.

## Page-tree semantics

The proposed page tree is the **capture discovery tree**, not a complete hyperlink graph:

1. `index.html` is the single root.
2. When a URL is first accepted into the breadth-first queue, its parent is the captured page whose rendered link first scheduled it.
3. A successfully captured, unique document becomes one tree node.
4. Redirect aliases point to the already selected node and never create another node.
5. Later links to an already scheduled or captured URL do not add another parent or create a cycle.
6. Failed and limit-omitted routes continue to affect snapshot quality and `missingUrls`, but are not represented as captured page nodes.

This definition is deterministic because the worker already walks documents breadth-first and links in rendered DOM order. It truthfully answers “how did this capture expand from the submitted page?” without pretending that an arbitrary cyclic website is naturally a tree.

A future link-graph feature could add explicit cross-link edges without changing these tree nodes. Cross-links are intentionally outside this iteration.

## Visualization evaluation

### LayerChart

LayerChart is the recommended implementation.

- Its official component catalog includes a `Tree` hierarchy layout specifically for parent-child branching and supports SVG and Canvas rendering.
- LayerChart 2 is built for Svelte 5 runes and snippets and no longer requires Tailwind, while remaining compatible with Tailwind 4.
- The hierarchy components are imported through `layerchart/hierarchy`, isolating the `d3-hierarchy` dependency from unrelated graph and geo packages.
- The core chart state provides responsive dimensions and opt-in pan/zoom, which avoids implementing another imperative viewport system.
- Svelte remains responsible for node markup and interaction, so the archive can use its own semantic controls, tokens, copy, and selected-state treatment rather than adopting a foreign visual system.

Relevant official references:

- [LayerChart Tree component](https://www.layerchart.com/docs/components/Tree)
- [LayerChart component scope and repository](https://github.com/techniq/layerchart)
- [LayerChart v1 to v2 / Svelte 5 migration](https://www.layerchart.com/docs/guides/migrations/v1-to-v2)
- [LayerChart 2 hierarchy sub-path and bundle isolation](https://next.layerchart.com/docs/releases/layerchart-2.0.0)

Risks and mitigations:

- LayerChart 2 is comparatively recent. Use only stable public exports (`Chart`/`ChartCore`, `Tree`, rendering primitives, and transform state), pin the resolved release in `bun.lock`, and validate SSR plus production build before integrating all three surfaces.
- LayerChart supplies layout and rendering primitives, not an accessibility model for this domain. The feature must provide semantic labels, keyboard-accessible selection, an outline alternative, and text details independently.
- Importing `Tree` still adds a runtime dependency. The documented hierarchy sub-path isolates `d3-hierarchy`; a short implementation spike will measure the actual production chunk before the full UI is committed.

### `d3-hierarchy` with custom Svelte SVG

`d3-hierarchy` directly offers a compact tidy-tree layout, hierarchy traversal, descendants, and parent-child links. It is the fallback if LayerChart's stable API or bundle output does not work in this project.

It would minimize abstractions but require custom responsive measurement, pan/zoom, viewport fitting, clipping, rendering, and interaction code. That extra code would recreate functionality LayerChart already provides and would be more expensive to maintain than the small bounded dependency.

Reference: [official D3 hierarchy documentation](https://d3js.org/d3-hierarchy).

### LayerChart Dagre or a force graph

Dagre and force-directed layouts are appropriate for a general directed graph with multiple parents, cycles, and cross-links. They are not the right primary model for the first-discovery tree:

- force positions are less deterministic and make depth harder to read;
- a visually dense web conflicts with the archive's provenance-first hierarchy;
- node movement and collision behavior add motion without adding information;
- the API would need to retain every hyperlink edge, which is not required by the current request.

If a later feature exposes the complete captured link graph, LayerChart's Dagre layout would be a better candidate than changing this tree into a force simulation.

### Cytoscape.js

Cytoscape.js provides a rich interactive graph surface, pan/zoom, and a breadth-first layout. It is designed for broader network-analysis use cases and uses an imperative graph instance. For at most 101 nodes in one strict tree, it would add substantially more API surface, integration code, and accessibility work than necessary.

Reference: [official Cytoscape.js layouts documentation](https://js.cytoscape.org/#layouts).

### Plain nested HTML

A semantic nested list is important as an accessible and compact alternative, but it is not sufficient as the only presentation. It does not provide the requested visual overview, depth relationships across branches, or useful viewport navigation for a larger capture.

### Decision

Use LayerChart's deterministic tidy-tree layout for the visual mode and render a project-owned semantic outline from the same data. Do not use a force graph. If the initial LayerChart build spike fails the compatibility or bundle check, fall back to `d3-hierarchy` without changing the contract or visual design.

## Public contract design

### New subresource

Add a small, read-only contract rather than expanding the shared list item:

```http
GET /api/snapshots/{snapshotId}/page-tree
GET /api/users/snapshots/{snapshotId}/page-tree
GET /api/admin/snapshots/{snapshotId}/page-tree
```

Visibility must mirror the existing detail resources:

- public endpoint: approved snapshots with a stored capture only;
- owner endpoint: current registered owner only;
- administrator endpoint: existing snapshot-administration permission boundary;
- unavailable, legacy, non-URL, corrupt, or unauthorized trees: `404`, preserving fail-closed behavior.

All three operations return the same `SnapshotPageTreeResponse`. The private variants forward no internal identifiers and use the existing cookie-authenticated server path. The frontend treats the tree as supplementary metadata: a transient tree failure must not hide an otherwise usable snapshot detail or archived page.

### Flat tree representation

Use a flat node list with parent paths rather than recursively nested JSON:

```json
{
  "rootPath": "index.html",
  "pages": [
    {
      "path": "index.html",
      "parentPath": null,
      "sourceUrl": "https://my-cool-site.net/",
      "title": "My cool site"
    },
    {
      "path": "pages/2c4f….html",
      "parentPath": "index.html",
      "sourceUrl": "https://my-cool-site.net/a",
      "title": "Section A"
    },
    {
      "path": "pages/91d8….html",
      "parentPath": "index.html",
      "sourceUrl": "https://my-cool-site.net/b",
      "title": "Section B"
    }
  ]
}
```

Proposed fields:

| Field | Meaning |
| --- | --- |
| `rootPath` | Stable path of the single root page; currently `index.html`. |
| `pages[].path` | Snapshot-local document path and stable node identity. |
| `pages[].parentPath` | Path of the first-discovery parent; `null` only for the root. |
| `pages[].sourceUrl` | Canonical final URL captured for this document. |
| `pages[].title` | Bounded, whitespace-normalized document title; `null` when absent. |

The array stays in deterministic breadth-first capture order. The frontend reconstructs nested children once and derives page count, maximum depth, descendants, and labels.

Flat storage avoids deeply recursive JSON. A legitimate chain can approach 101 pages, while default JSON serializer depth limits are commonly lower. It also produces a simple OpenAPI schema and permits strict referential validation before data reaches the client.

The response intentionally excludes object-store keys, hashes, redirect aliases, missing asset URLs, internal user identifiers, and capability tokens. It also does not duplicate `contentUrl` for every page.

## Manifest and compatibility design

Add an optional `pageTree` object to `SnapshotManifest` version 4 containing the same flat structural data. The capture worker writes it for new URL captures. Existing version-4 manifests deserialize with `pageTree == null`; HTML, MHTML, and WebArchive imports remain unchanged and do not synthesize a potentially misleading hierarchy.

Do not increment `SnapshotManifest.CurrentVersion` for this additive optional field. The repository currently requires an exact version match, so an unconditional version 5 would make all existing version-4 snapshots unreadable without a storage migration. The optional field is backward-compatible and does not change content authorization.

Before returning a stored page tree, the manifest reader must validate all invariants:

1. page count is between 1 and the configured root-plus-linked-page bound;
2. paths are normalized, unique, and present exactly once as `root-document` or `nested-document` manifest resources;
3. `rootPath` identifies the only node with `parentPath == null` and a `root-document` role;
4. every other parent exists and has a document role;
5. all nodes are reachable from the root and no cycle exists;
6. source URLs are absolute HTTP(S) URLs and stay within contract length limits;
7. titles are normalized and bounded during capture.

Invalid optional tree metadata makes the page-tree subresource unavailable; it must not make the archived content itself unreadable. The existing manifest remains the sole authorization source for content delivery.

## Capture changes

Affected components:

- `backend/Archive.Worker/Capture/Browser/BrowserCaptureEngine.cs`;
- `backend/Archive.Worker/Capture/Browser/Crawl/CapturedDocument.cs`;
- `backend/Archive.Core/Storage/SnapshotManifest.cs`;
- focused tree storage/validation types beside the manifest.

Planned behavior:

1. Replace the queue's bare URL with a small pending-route value containing the candidate URL and the discovering `CapturedDocument`.
2. Record a captured document's first-discovery parent only after it becomes a unique successful document.
3. Capture `document.title`, normalize whitespace, discard control characters, and cap its stored length.
4. After final document paths are assigned, map document references to `path`/`parentPath` records in breadth-first order.
5. Write the optional flat page tree into `manifest.json` together with the resources.
6. Preserve current redirect de-duplication: if a candidate resolves to an existing document, it creates neither another resource nor another tree node.

This adds no crawl requests, concurrency, database columns, screenshots, content objects, or changes to completeness semantics.

## Repository and API changes

### Manifest read service

Extract the repeated “load and deserialize the active manifest” operation into one focused core service used by the public and private repositories. It should:

- read `manifest.json` only after the repository has resolved a visibility-authorized `ContentPrefix`;
- retain exact current-version checks;
- validate and return the optional page tree separately from content authorization;
- log malformed or inconsistent metadata without exposing storage details.

This small extraction is required to avoid duplicating security-sensitive object-store parsing across three endpoint scopes. It must not refactor unrelated repository behavior.

### Contracts and controllers

Add:

- `SnapshotPageTreeResponse`;
- `SnapshotPageResponse`;
- mapping from the validated core tree model;
- one endpoint in each existing snapshot controller scope;
- OpenAPI summaries, descriptions, and `200`/`401`/`403`/`404` responses matching their current controller conventions.

Regenerate `frontend/src/lib/api/` with the established `bun run generate:api` workflow while the backend is running. Do not hand-edit generated API types.

## Frontend component design

### Placement

Create one archive-specific component, not a generic UI primitive:

```text
frontend/src/lib/components/archive/SnapshotPageTree.svelte
frontend/src/lib/components/archive/snapshot-page-tree.ts
```

The component may be reused by three archive features because it consumes `SnapshotPageTreeResponse` and contains archive-specific URL/path copy. It does not belong in `components/ui`.

No new application route or standalone view is planned, so the required new-route HTML mockup rule is not triggered. The implementation must still be reviewed in all three real routes.

### Visual direction

Use a left-to-right tidy tree with the character of a capture register rather than a generic analytics chart:

```text
Captured pages · 3                         [−] [+] [Fit] [Diagram | Outline]
──────────────────────────────────────────────────────────────────────────

  ●  /                         ─────────┬────────  ○  /a
  Root · My cool site                   │
                                       └────────  ○  /b

──────────────────────────────────────────────────────────────────────────
Selected page
/a
https://my-cool-site.net/a
Captured as pages/2c4f….html · Depth 1 · 0 children
```

Design rules:

- fine Border Trace links and quiet surfaces;
- root marked with Time Copper plus the visible label `Root`;
- other pages use Register Blue plus a distinct outlined node shape;
- selected page uses a 3px mark, stronger outline, `aria-pressed`, and a details region;
- path labels use IBM Plex Mono; page titles and supporting text use Source Sans 3;
- no gradients, physics, pulsing nodes, perpetual animation, or glass effects;
- short 120–180 ms fit/selection transitions only, removed under `prefers-reduced-motion`;
- full source URL remains visible and copyable in the selected-page detail rather than being available only in a tooltip.

The “distinctive” quality comes from a precise, explorable topology, direct labels, and the archive's register-line visual language—not decorative effects.

### Interaction

- Diagram nodes are real keyboard-focusable buttons rendered through Svelte-controlled markup.
- Selecting a node updates a persistent detail block with title, full source URL, archived path, depth, and child count.
- Zoom in, zoom out, and fit-all are labeled buttons with at least 44px targets.
- Pointer dragging pans the diagram only inside its bounded viewport; it must not block normal page scrolling on touch devices.
- The initial view fits all nodes. Selection must remain after fitting or switching modes.
- The first iteration does not navigate the snapshot iframe or add a `?page=` URL contract. It displays and inspects the tree only, matching the requested scope.

### Accessible outline

The same normalized model also renders as a nested semantic list in **Outline** mode:

- depth conveyed structurally and in text;
- the same selection buttons and details region;
- no information available only by color, pointer hover, or chart geometry;
- branch counts and full URLs announced in accessible labels;
- deterministic keyboard tab order following breadth-first API order for the diagram and document order for the outline.

The view choice is local component state and does not need persistence. On narrow screens the outline is the default visual presentation through responsive markup/CSS, while the user can still opt into the horizontally pannable diagram.

### Bounded rendering

The crawler already limits the data to at most 101 document nodes. SVG is therefore appropriate and preserves sharp text. Canvas is unnecessary for this bound and would make semantic node interaction harder. The layout should compute a bounded virtual width/height from node count and depth, then rely on the chart viewport for fitting and pan/zoom.

## Integration in requested routes

### Owner and administrator detail

Extend `loadSubmission` to fetch detail, history, and the scope-matching page tree concurrently. A tree read failure returns an unavailable flag instead of failing the page.

Add a full-width **Captured pages** section in `SubmissionDetail.svelte` after the detail/preview columns and before moderation/resubmission/history actions. This makes provenance visible without interrupting the main record summary and automatically covers:

- `/account/submissions/{snapshotId}`;
- `/admin/snapshots/{snapshotId}`.

When the tree is unavailable, show a restrained status block explaining that page hierarchy metadata is unavailable for this capture. In-progress captures should say that the page tree becomes available after capture rather than implying an error.

### Snapshot viewer dialog

Extend the existing dialog tabs to:

- **Info**;
- **Pages**;
- **History**.

The Pages tab uses a direct Lucide network/tree icon, the shared `SnapshotPageTree` component, and the same unavailable state. `+page.server.ts` requests the public tree for approved snapshots or the matching owner/admin tree for an authorized private snapshot. Page-tree failure remains supplementary, like history failure, and never hides the iframe.

The dialog already uses a large bounded layout and sticky tabs. The page-tree panel receives an explicit viewport height so chart measurement is stable after the lazy tab mounts.

## Data loading and error behavior

- Every page-tree fetch uses `cache: "no-store"`, `redirect: "error"`, and the existing ten-second timeout.
- Cookies are forwarded only to the configured backend and only for the private endpoints.
- `401`/`403` behavior on owner/admin record routes remains authoritative for the main detail request.
- A page-tree `404`, timeout, invalid JSON, or server error is converted to `pageTree: null` plus `pageTreeUnavailable: true` for the supplementary UI.
- The snapshot viewer must reuse the already selected public/private scope; it must not probe a broader endpoint after a successful narrower authorization decision.
- The browser receives only the generated contract, never `manifest.json` or object-store credentials.

## Dependency changes

After approval and a successful spike:

- add `layerchart` as a frontend runtime dependency;
- add `d3-hierarchy` as a direct runtime dependency if the public LayerChart Tree API requires consumers to construct the hierarchy explicitly;
- update `frontend/bun.lock` only through Bun;
- use the hierarchy sub-path import and avoid Svelte UX or unrelated LayerChart graph packages.

Do not add Cytoscape, Dagre, a second icon library, or a general graph-state dependency.

## Tests and validation

### Backend capture and storage

Extend multi-route capture coverage for:

1. root, `/a`, and `/b` form the expected flat tree;
2. nested discovery records the correct first parent and breadth-first order;
3. a cycle back to root does not create another node;
4. the same target discovered by two pages keeps the first scheduled parent;
5. redirects and aliases do not duplicate nodes;
6. failed or limit-omitted pages are absent from the tree while quality remains `INCOMPLETE`;
7. titles are normalized, bounded, and safely serialized;
8. new URL captures keep manifest version 4 and old version-4 manifests without `pageTree` remain readable;
9. file-import manifests remain valid with no synthetic page tree.

### Repository and API

Add focused tests for:

1. public tree available only for an approved stored snapshot;
2. owner tree available only to its owner;
3. administrator tree requires existing snapshot permissions;
4. legacy/missing/corrupt trees return `404` without affecting content delivery;
5. invalid parent paths, duplicate paths, cycles, non-document resources, and unreachable nodes fail closed;
6. response omits manifest-only fields and capability data;
7. generated OpenAPI expresses the new flat response and all three endpoints.

### Frontend and component checks

- normalize a flat response into a tree with duplicate/orphan/cycle safeguards even though the backend validates it;
- validate Svelte files with the Svelte MCP/autofixer if available;
- run `bun run check`, `bun run build`, and `bun run build:storybook`;
- run the existing lint check and report pre-existing repository-wide formatting failures separately if they remain;
- verify `prefers-reduced-motion`, keyboard focus, visible selected state, URL wrapping, and 200% text enlargement.

The archive-specific tree is not a generic `components/ui` primitive, so no neutral UI-library story is required. If the implementation extracts a genuinely generic viewport control, that primitive must follow the existing Storybook contract separately.

### Full project checks

```powershell
dotnet build backend/Archive.slnx -m:1
dotnet test backend/Archive.slnx -m:1
Set-Location frontend
bun run generate:api
bun run check
bun run build
bun run build:storybook
bun run lint
Set-Location ..
git diff --check
git status --short
```

Generate the API client only after the backend is running with all required local services.

### Manual browser validation

Create a fresh multi-route snapshot after the manifest change; an existing snapshot cannot acquire missing discovery metadata retroactively. Verify:

- public viewer dialog: Info → Pages → History tab order and keyboard operation;
- owner detail and administrator detail show the identical tree with the correct access boundary;
- a branching fixture, a deeper chain, and a root-only URL capture;
- light and dark themes;
- desktop and mobile viewports, including the outline-first narrow layout;
- fit, zoom, pan, selection, full URL wrapping, and reduced motion;
- unavailable states for a legacy URL snapshot and a file import;
- the archived iframe and direct in-snapshot navigation continue to work unchanged;
- browser network activity never requests source-site pages merely to render the tree.

## Expected impact

- New URL snapshots gain a small amount of manifest metadata: one bounded record per captured document.
- The API performs one extra manifest read only when a page-tree endpoint is requested; search/list endpoints remain unchanged.
- The three frontend detail surfaces perform one additional parallel supplementary request.
- The frontend production bundle gains LayerChart's core/tree path and `d3-hierarchy`; the exact chunk delta must be recorded during implementation.
- Existing captures remain replayable. They show an unavailable page-tree state because the first-discovery relationship cannot be reconstructed honestly.
- Capture duration, resource count, storage object layout, moderation, and public content security remain unchanged except for the few manifest bytes counted in total storage.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| A cyclic website is misrepresented as a natural tree | Define and label the data as the deterministic first-discovery tree; de-duplicate later links. |
| Existing snapshot lists become very large | Keep the tree in a dedicated detail subresource. |
| Deep trees exceed JSON recursion limits | Persist and expose a flat list with `parentPath`. |
| Corrupt metadata creates cycles or references unauthorized objects | Validate paths, resource roles, uniqueness, reachability, and acyclicity before mapping. |
| Legacy snapshots stop working | Keep manifest version 4 and make `pageTree` optional; content delivery ignores absent tree metadata. |
| Chart-only UI is inaccessible | Provide focusable node selection, persistent textual details, and a semantic outline mode. |
| Diagram is unreadable on mobile | Default to outline presentation at narrow widths and keep diagram pan/zoom opt-in. |
| New chart dependency adds unnecessary code | Use LayerChart hierarchy sub-path only, measure the production chunk, and retain a `d3-hierarchy` fallback. |
| Visual treatment conflicts with the archive | Use existing editorial tokens, direct labels, quiet links, and no force animation or decorative effects. |
| Tree fetch failure hides snapshot content | Treat the tree as supplementary metadata on every frontend surface. |

## Explicit non-goals

- Capturing additional routes or changing crawl limits.
- A complete site hyperlink graph or visualization of every cross-link.
- Showing asset dependencies such as images, scripts, or styles in the page tree.
- Retrofitting inferred trees onto old snapshots.
- Navigating or replacing the archived iframe when a tree node is selected.
- Per-page screenshots, per-page moderation, or separate snapshot records.
- Database persistence or a database migration for the tree.
- Changes to snapshot search, tags, history, moderation, authentication, or content CSP.
- A general graph editor or a new frontend route.

## Implemented sequence

1. Keep the work on `codex/multi-route-snapshot-capture`, because the contract depends directly on the unmerged multi-route manifest and is intended to extend the same draft pull request.
2. Add capture-tree tests and the optional flat manifest model without changing version 4.
3. Record first-discovery parents and bounded document titles in the worker.
4. Add the validated manifest page-tree reader and repository access methods.
5. Add public, owner, and administrator page-tree contracts/endpoints with API tests.
6. Regenerate the frontend client.
7. Add LayerChart in a small production-build spike; record bundle impact and use the documented fallback only if necessary.
8. Implement the archive-specific diagram/outline component and unavailable state.
9. Integrate the shared component into `SubmissionDetail` and the viewer dialog's Pages tab.
10. Run automated checks and validate the three requested routes in a real browser on desktop/mobile and light/dark themes.
11. Review the final diff for task-only changes and create logically separated semantic commits, for example:
    - `feat: expose snapshot page trees`
    - `feat: visualize captured page trees`
    - `docs: document snapshot page trees`

## Approved decisions embodied by this plan

Approval authorizes the following scoped decisions:

- model a deterministic first-discovery tree rather than a complete cyclic hyperlink graph;
- persist optional flat page-tree metadata in manifest version 4 for new URL captures only;
- expose it through three scope-matched page-tree subresources rather than inflating `SnapshotResponse` and list responses;
- use LayerChart's tidy-tree hierarchy with a semantic outline alternative, subject to the stated build/bundle spike;
- add the page-tree section to the shared owner/admin detail and a third Pages tab to the snapshot viewer dialog;
- leave old and non-URL snapshots readable and show an explicit unavailable state;
- make no database, crawl-scope, replay-security, or iframe-navigation change.
