# Plan: Browser-independent interactive snapshot replay

**Status:** Approved by the user on 2026-10-29; implementation may begin

**Prepared:** 2026-10-28

**Scope:** Remove the Chromium/Safari playback split. A stored document executes its archived JavaScript in every supported browser; documents without JavaScript remain non-executable by construction.

## 1. Product decision

Playback must be determined by the snapshot, not by the browser. The current static-versus-interactive client gate will be removed completely:

- Safari, Firefox, Chromium, and other supported browsers receive the same canonical document and the same script-capable replay policy;
- no user-agent or client-hint check decides whether archived JavaScript runs;
- `Connection-Allowlist` remains an unconditional, additive defense where a browser implements it, but it is no longer a prerequisite for replay;
- the UI will not expose a degraded “static playback” mode;
- archived code remains restricted to resources captured into the same snapshot and receives no live-site, Archive-session, storage, form, popup, download, device, or top-navigation privileges.

This is functional parity, not permission for archived code to use the live network. Unsupported or unsafe web capabilities continue to be blocked consistently in every browser.

## 2. Current implementation and root cause

The canonical stored HTML already contains preserved scripts, rewritten resource references, and a first-party replay shim. The browser split happens only at delivery:

1. `SnapshotInteractionCapabilityPolicy` parses `Sec-CH-UA`.
2. Only configured Chromium brands at or above the configured version receive a script-capable CSP and `Connection-Allowlist`.
3. Safari sends no matching Chromium client hint and receives `script-src 'none'`.

Safari therefore does not have a snapshot-JavaScript defect; the API deliberately disables all archived scripts for it. The split also affects Firefox and any future browser without the Chromium hint.

The existing containment layers that are independent of this gate are:

- a dedicated, credential-free snapshot origin;
- an iframe with `sandbox="allow-scripts"`, without same-origin, forms, popups, downloads, storage, or top-navigation privileges;
- snapshot-scoped CSP sources and `form-action 'none'`;
- manifest/path/destination validation in the content endpoint;
- normalization of captured HTML, CSS, JavaScript, import maps, frames, and resource URLs;
- a replay shim that resolves Fetch/XHR/DOM/Worker requests only to captured resources and blocks live transports;
- no service workers and a restrictive Permissions Policy.

## 3. Proposed delivery contract

### 3.1 One policy for all clients

Delete `SnapshotInteractionCapabilityPolicy` and remove `AllowedBrands` and `MinimumChromiumMajorVersion` from `SnapshotContentOptions`, configuration, validation, and documentation.

`SnapshotContentPolicy` will always emit the current interactive document policy for canonical root and nested HTML. It will also emit the snapshot-scoped `Connection-Allowlist` unconditionally. Browsers that implement the header enforce the additional restriction; browsers that do not simply ignore the unknown header without losing JavaScript.

The document response no longer varies by `Sec-CH-UA`, so `Vary: Sec-CH-UA` is removed. This also eliminates the possibility that caches mix two playback representations.

Static API-origin downloads and original-source downloads remain non-executable. Only canonical manifest-authorized replay documents on the isolated snapshot origin use the script-capable policy.

### 3.2 Parent-controlled navigation boundary

The snapshot page will set a route-specific parent CSP whose `frame-src` contains only the origin parsed from the API-provided `contentUrl`. This prevents the nested browsing context from navigating itself to a live external origin in browsers that do not enforce `Connection-Allowlist`.

The origin is derived server-side from the already authorized content URL; it is not accepted from a query parameter. Existing `frame-ancestors 'none'` protection for the Archive UI remains. The iframe keeps `sandbox="allow-scripts"` and `referrerpolicy="no-referrer"`.

### 3.3 Cross-browser replay-shim hardening

Extend `SnapshotRuntimeShim` before removing the gate:

- preserve captured-only Fetch and XHR resolution with `GET`/`HEAD`, omitted credentials, and no redirects;
- block `navigator.sendBeacon` as well as WebSocket, EventSource, WebTransport, SharedWorker, WebRTC, service-worker registration, popups, and form submission;
- make blocked transport replacements non-writable and non-configurable where the platform permits it;
- reject page-created `blob:` workers. Captured dedicated workers continue to run through the existing archive bootstrap, but an arbitrary blob worker must not create an uninstrumented realm;
- keep nested stored documents normalized with their own shim;
- reject uncaptured dynamic frame targets and keep captured frame URLs inside the snapshot scope;
- remove DNS-prefetch/preconnect/prerender hints and add `X-DNS-Prefetch-Control: off` on replay responses;
- retain the current denial of service/shared workers at the HTTP destination boundary.

These restrictions apply identically in Safari and Chromium. `Connection-Allowlist` becomes one defense-in-depth layer instead of the switch controlling functionality.

### 3.4 Content-driven diagnostics

Retain `SnapshotManifest.Interaction.ContainsExecutableContent` and normalization diagnostics for capture quality and tests, but do not use browser identity to change delivery. A page with no scripts or event handlers naturally executes none; a page with stored executable content receives the same replay behavior everywhere.

### 3.5 Existing snapshot compatibility

The runtime shim is embedded into stored HTML during capture, so removing the browser gate must not expose older documents with the previous shim unchanged. Increment the document normalizer identifier to `archive-snapshot-v3` and handle existing `archive-snapshot-v2` documents at the manifest-authorized read boundary:

- rebuild an older root or nested document through `SnapshotDocumentNormalizer` using its manifest source URL and snapshot-relative resource map;
- replace the embedded v2 shim with the current v3 shim before the response is returned;
- retain already rewritten local references and aliases while restoring the captured-only runtime boundary;
- fail closed when an unknown normalizer or a document cannot be safely normalized;
- serve v3 documents directly unless their manifest records a normalization diagnostic, in which case the same safe re-normalization path is applied.

This response-time compatibility path does not mutate preserved objects or their provenance hashes. New captures persist v3 directly, while existing v2 snapshots remain readable and executable without a bulk object-storage migration.

## 4. Expected changes

### Backend/API

- Remove `backend/Archive.Api/SnapshotContent/SnapshotInteractionCapabilityPolicy.cs` and its DI registration.
- Simplify `SnapshotContentPolicy.Apply` and its callers to a single canonical document policy.
- Remove Chromium gate settings from `SnapshotContentOptions`, appsettings files, environment examples, and option tests.
- Add the cross-browser response headers and keep manifest/destination checks unchanged.
- Harden `SnapshotRuntimeShim` and its normalization tests.
- Re-normalize stored v2 documents at the manifest-authorized repository boundary before enabling their scripts.

### Frontend

- In the existing `/snapshots/[snapshotId]` server load, derive the allowed frame origin from `contentUrl` and set the route-specific `frame-src` CSP.
- Keep the existing viewer and iframe.

### Documentation

Update `README.md`, `docs/architecture.md`, `docs/interactive-snapshots.md`, `docs/configuration.md`, `docs/deployment.md`, and `docs/development.md` to describe one browser-independent policy.

No new route or standalone view is introduced, so no HTML mockup is required.

## 5. Validation strategy

Add focused automated coverage for:

- identical script-capable CSP with missing, Safari-like, Firefox-like, malformed, and Chromium `Sec-CH-UA` values;
- unconditional snapshot-scoped `Connection-Allowlist` and absence of `Vary: Sec-CH-UA`;
- public and capability URLs using the same policy;
- uncaptured Fetch/XHR/media/frame requests, beacon, WebRTC, WebTransport, WebSocket, EventSource, form, popup, service worker, arbitrary blob worker, and external navigation being blocked;
- captured scripts, modules, Fetch/XHR, media, nested documents, and captured dedicated workers continuing to function.

Perform real-browser verification in current Safari on macOS/iOS plus Chromium and Firefox. Playwright WebKit is useful for regression coverage but is not accepted as the only Safari validation.

Run:

```powershell
dotnet build backend/Archive.slnx
dotnet test backend/Archive.slnx -m:1

Set-Location frontend
bun run check
bun run lint
bun run build
```

## 6. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Safari ignores `Connection-Allowlist` | Enforce isolated origin, iframe sandbox, parent `frame-src`, CSP, URL rewriting, destination validation, and hardened shim in every browser |
| Archived code opens an uninstrumented execution realm | Reject arbitrary blob workers and uncaptured dynamic frames; normalize every stored nested document |
| A live navigation leaks data | Parent route CSP restricts the iframe to the snapshot origin; sandbox continues to block top navigation and popups |
| A captured script expects a deliberately blocked live API | Mark the capture incomplete where appropriate; do not restore live network access |
| Removing client variation changes caches | Remove `Vary: Sec-CH-UA`; canonical documents remain `no-store` |

## 7. Version-control execution

Before implementation, fetch and fast-forward the local main branch without overwriting local work, then create `codex/cross-browser-snapshot-replay`. Keep this work independent from video capture and private visibility.

Proposed commits:

- `fix: enable cross-browser snapshot scripts`
- `fix: harden archived runtime containment`
- `docs: document browser-independent replay`

## 8. Approval record

The user approved implementation on 2026-10-29. Approval confirms that snapshot JavaScript is enabled independently of browser identity while the same captured-only API restrictions apply everywhere. Existing v2 documents are upgraded at the read boundary so browser parity does not weaken containment for previously captured snapshots.
