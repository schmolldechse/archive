# Interactive snapshots

## Behavior

Archived snapshots preserve client-side interaction in addition to their visual document. A reader can use captured disclosure controls, tabs, local validation, filtering, animations, modules, canvas rendering, bounded client-side routing, and requests for resources stored in the same snapshot.

This behavior applies to every completed snapshot, independent of moderation status. Public snapshots and access-controlled owner or administrator previews use the same canonical document and the same isolated execution boundary. Moderation continues to determine who may access a snapshot; it does not determine whether its stored JavaScript is executable.

Archived code must remain disconnected from the live website and from Archive application privileges. It may read only resources captured into its own snapshot and must not gain cookies, authentication, browser storage, form submission, persistent workers, pop-ups, downloads, device permissions, top-level navigation, or arbitrary network access. Dedicated workers are available only through the bounded replay behavior described below.

## Snapshot model

A snapshot remains the only content model. Each completed snapshot has one active object generation selected by `Snapshot.ContentPrefix` and contains:

```text
snapshots/{snapshotId}/
├── index.html
├── pages/...
├── screenshot.png
├── manifest.json
├── assets/...
├── frames/...
└── source.{html|mhtml|webarchive}
```

`index.html` is the canonical document presented to readers. URL captures store recursively discovered same-origin pages below `pages/` as nested documents of the same snapshot. The immutable source object provides provenance for the seed document. The manifest authorizes stored resources and records their hashes, media types, browser roles, source URLs, capture diagnostics, and source-to-document relationship. The current manifest contract and browser security boundary determine how every stored document is served.

## Capture and normalization

The worker preserves scripts and inline event handlers while rewriting captured dependencies to snapshot-local paths.

For URL captures it retains the final successful seed response before application scripts mutate the page. That response becomes `source.html` and is the input for the canonical root document. After the rendered page settles, the worker discovers anchor and area links on the seed's effective origin and captures them recursively in serial breadth-first order. Every linked HTML response is normalized as a `nested-document`; a global URL map keeps navigation and shared dependencies inside the snapshot. If a response cannot be retained or parsed, the bounded rewritten browser document remains usable and the manifest records the diagnostic.

The linked-page queue is bounded and de-duplicated after URL normalization. Query strings remain part of route identity, fragments share one stored document and are preserved on rewritten links, and external origins, subdomains, downloads, forms, and non-HTTP schemes are not crawled. A failed or omitted in-scope route makes the stored capture incomplete. Hard duration, resource, and storage limits retain their discard behavior.

For HTML, MHTML, and WebArchive uploads, the uploaded bytes remain immutable source material. The importer decodes the archive, builds a deterministic resource map, produces the canonical document, and keeps JavaScript disabled only in the worker context used to generate the screenshot.

Native finite media follows the same self-contained model. During URL capture the worker briefly loads muted `video` and `audio` elements in the root document and captured frames so dormant direct sources can be requested. For MHTML uploads, which may reference media bytes that the exporting browser omitted, the importer may retrieve only explicit `video`, `audio`, `source`, `track`, and video-poster HTTP(S) references. Each redirect remains subject to the public-network guard, requests carry no source credentials or referrer, and only complete `200` responses that fit the existing resource and storage limits are retained. The original uploaded archive remains unchanged.

Direct MP4, WebM, Ogg, MP3, AAC, WAV, Opus, FLAC, poster images, and WebVTT tracks can therefore replay when their complete resources were captured and the reader's browser supports the stored codec. A failed, partial, oversized, or unavailable direct resource is recorded as missing and makes the snapshot incomplete. Adaptive HLS/DASH graphs, live streams, Media Source Extensions assembled from uncaptured segments, WebRTC or device streams, DRM/EME, and legacy plugin objects are outside the guaranteed capture path. They remain blocked from contacting the live service during replay.

Normalization performs the following operations:

1. Remove source-provided CSP, refresh, referrer, integrity, credentialed CORS, ping, DNS-prefetch, preconnect, prefetch, and prerender controls that conflict with archive isolation.
2. Rewrite captured document, stylesheet, image, font, media, frame, script, module, JSON, model, and binary references to safe relative snapshot paths.
3. Rewrite bounded JavaScript literal forms, including static module imports, dynamic imports, `new URL(..., import.meta.url)`, `fetch`, and XHR reads, when their absolute target is an exact captured resource.
4. Insert a deterministic `<base>` for the stored document location.
5. Insert a fixed snapshot shim before archived scripts. The shim resolves exact captured runtime URLs, forces credential omission, restricts methods to `GET` and `HEAD`, and disables selected connection capabilities as defense in depth. It rejects page-created `blob:` workers and loads exact captured worker scripts through an isolated, first-party `blob:` bootstrap with the same runtime guards.
6. Assign every served object one browser role: `root-document`, `nested-document`, or `asset`.
7. Hash the source, canonical document, and every manifest-authorized resource with SHA-256.
8. Record missing resources and normalization diagnostics without discarding an otherwise readable snapshot.

When normalization is incomplete, the worker retains the safest rewritten document it can produce and records the diagnostic. The isolated response boundary continues to restrict its execution and network access.

## Manifest contract

The current snapshot manifest is version `4`. Its top-level structure is:

```json
{
  "version": 4,
  "provenance": {
    "derivedDisplayDocument": true,
    "sourceObjectKey": "snapshots/.../source.html",
    "sourceSha256": "...",
    "indexSha256": "...",
    "sourceUrl": "https://example.test/",
    "sourceMimeType": "text/html",
    "capturedAt": "2026-10-24T00:00:00Z",
    "documentState": "original-response",
    "redirectChain": []
  },
  "interaction": {
    "normalizer": "archive-snapshot-v3",
    "containsExecutableContent": true,
    "diagnostic": null
  },
  "resources": [],
  "missingUrls": [],
  "missingUrlCount": 0,
  "incomplete": false
}
```

The API serves a resource only when:

- the active generation has a readable current manifest;
- the requested relative path is safe;
- exactly one manifest entry matches the path;
- the entry object key exactly equals the active content prefix plus that path;
- the object exists;
- the browser destination is valid for the recorded resource role.

Original source files are downloads and are not executable manifest resources.

## Isolated snapshot origin

Archived JavaScript runs on a dedicated credential-free snapshot origin served by the existing API process. In development, the API can use `localhost` while snapshot content uses `127.0.0.1` on the same port. Production should use a separate registrable domain where practical.

Configuration uses the `SnapshotContent` section:

```json
{
  "SnapshotContent": {
    "PublicOrigin": "https://snapshots.example.net",
    "PrivateAccessTokenLifetimeMinutes": 60
  }
}
```

The origin must differ from the frontend and authenticated API origins, use HTTPS outside development, and remain outside the authentication session-cookie domain.

Before routing a request received on this origin, the API:

- removes `Cookie` and `Authorization` headers;
- accepts only `GET`, `HEAD`, and CORS preflight requests;
- exposes only `/api/snapshots/{id}/content/{path}`;
- rejects metadata, authentication, administration, mutation, and unrelated API paths.

The same controller and repository resolve public and access-controlled snapshot content.

## Access-controlled snapshots

Owner and administrator authorization continues through the authenticated API origin. After an authorized detail request succeeds, including for an approved owner-private snapshot, the API issues a short-lived data-protected bearer capability bound to exactly one snapshot and the authorized account.

The returned `contentUrl` places the capability in the content path:

```text
https://snapshots.example.net/api/snapshots/{id}/content/~{capability}/index.html
```

Relative document and asset URLs retain the same capability prefix. The snapshot origin validates the capability without cookies, then revalidates the bound account against the snapshot's current owner, registration state, role, and permission overrides before every resource read. Invalid, expired, modified, snapshot-mismatched, or no-longer-authorized capabilities return `404`.

The capability lifetime defaults to 60 minutes and is configurable. Metadata responses containing it remain private and non-cacheable. Reloading an authorized snapshot page obtains a new capability.

Stored, approved snapshots whose owner has not marked them private keep the ordinary path without a capability:

```text
https://snapshots.example.net/api/snapshots/{id}/content/index.html
```

Moderation and owner visibility determine public publication, not the replay policy. Once access has been established, every completed snapshot uses the same browser-independent content policy.

## Browser execution policy

The frontend embeds every snapshot with:

```html
<iframe sandbox="allow-scripts"></iframe>
```

`allow-same-origin` is intentionally absent. The document receives an opaque origin and cannot read application cookies or storage even if it is opened from an authorized private view.

The document response supplies a restrictive policy whose sources are scoped to the exact public or capability-prefixed snapshot content path. It permits captured scripts, inline historical scripts, styles, images, fonts, media, frames, same-snapshot reads, and the first-party `blob:` bootstrap used for captured dedicated workers while denying objects, forms, persistent workers, and connections outside that path.

The viewer delegates `autoplay` to the opaque snapshot frame but does not relax the browser's own audible-media or user-gesture rules. Stored `autoplay`, `loop`, `muted`, `playsinline`, `preload`, and `controls` attributes remain intact. When an archived autoplay attempt is rejected and the original direct media element has no controls, the replay shim exposes native controls as a fallback. Camera, microphone, encrypted media, external frames, and live network access remain denied.

Page-created `blob:` workers are rejected so archived code cannot create an uninstrumented execution realm. The replay policy permits WebAssembly compilation through the narrow `wasm-unsafe-eval` source expression without enabling JavaScript `eval`. A dedicated worker requested through an exact captured source URL is resolved to its snapshot asset and started through a short-lived, first-party `blob:` bootstrap. That bootstrap installs the captured-resource resolver before loading classic or module worker code. It limits Fetch and XHR to captured resources with `GET` or `HEAD`, omits credentials, rejects redirects, and blocks WebSocket, EventSource, WebTransport, Shared Worker, and WebRTC constructors as defense in depth. Shared Workers and Service Workers remain unavailable, and their request destinations are rejected by the content endpoint.

New captures store the runtime shim with normalizer `archive-snapshot-v3`. Manifest-authorized reads upgrade existing v1 and v2 root and nested documents in memory before delivery; preserved objects and provenance hashes remain unchanged. Documents with an unknown normalizer or an unsafe normalization failure fail closed.

Additional response controls include:

- `X-Content-Type-Options: nosniff`;
- `Referrer-Policy: no-referrer`;
- a restrictive `Permissions-Policy`;
- credential-free CORS for opaque-origin module and resource reads;
- `Cache-Control: no-store` for documents;
- `X-DNS-Prefetch-Control: off` and removal of captured DNS, preconnect, prefetch, and prerender hints;
- role-specific navigation checks for HTML, SVG, XML, and other active formats.

Immutable assets are served with a strong manifest-hash ETag and support one closed, open-ended, or suffix byte range on both public and capability URLs. Satisfiable requests return `206` with an exact `Content-Range`; malformed, multiple, and unsatisfiable ranges return `416`. This gives native media elements and PDF viewers the seeking behavior they expect without treating a partial capture as a complete archived resource. Runtime-upgraded HTML documents do not advertise range support.

## Browser-independent containment

Content Security Policy does not cover every connection channel available to untrusted JavaScript. In particular, navigation, redirects, DNS-related behavior, WebRTC, and newer connection APIs require a broader browser-enforced boundary.

The API sends `Connection-Allowlist` scoped to the current snapshot on every replay document. Browsers that implement it gain an additional restriction for redirects and WebRTC; browsers that do not implement it still run the same archived JavaScript behind the isolated origin, opaque iframe sandbox, snapshot-scoped CSP, captured-resource shim, destination checks, and parent-page `frame-src` boundary.

No user-agent or client hint changes this policy, and replay responses do not vary on `Sec-CH-UA`. A snapshot containing executable content therefore behaves consistently in Safari, Firefox, and Chromium. A snapshot without executable content naturally executes none.

## Public API

`SnapshotResponse` keeps its existing shape. `contentUrl` remains the single entry URL and is null until capture storage completes.

- Public metadata returns a public snapshot-origin URL.
- Authorized owner and administrator metadata returns a short-lived capability URL on the same origin.
- The frontend embeds the returned canonical snapshot URL directly.

The OpenAPI description documents these semantics, and generated frontend types continue to be produced through `bun --bun run generate:api` while the backend is running.

## Deployment requirements

Deploy the API, worker, frontend, isolated snapshot-origin routing, and persistent API data-protection key ring together. Verify both public and access-controlled snapshot URLs through the configured credential-free origin. The reverse proxy must preserve the snapshot host and forward it to the API container without adding authentication or session cookies. See [Deployment](deployment.md) for the complete origin and proxy contract.
