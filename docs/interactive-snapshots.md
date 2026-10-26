# Interactive snapshots

## Behavior

Archived snapshots preserve client-side interaction in addition to their visual document. A reader can use captured disclosure controls, tabs, local validation, filtering, animations, modules, canvas rendering, bounded client-side routing, and requests for resources stored in the same snapshot.

This behavior applies to every completed snapshot, independent of moderation status. Public snapshots and access-controlled owner or administrator previews use the same canonical document and the same isolated execution boundary. Moderation continues to determine who may access a snapshot; it does not determine whether its stored JavaScript is executable.

Archived code must remain disconnected from the live website and from Archive application privileges. It may read only resources captured into its own snapshot and must not gain cookies, authentication, browser storage, form submission, workers, pop-ups, downloads, device permissions, top-level navigation, or arbitrary network access.

## Snapshot model

A snapshot remains the only content model. Each completed snapshot has one active object generation selected by `Snapshot.ContentPrefix` and contains:

```text
snapshots/{snapshotId}/
├── index.html
├── screenshot.png
├── manifest.json
├── assets/...
├── frames/...
└── source.{html|mhtml|webarchive}
```

`index.html` is the canonical document presented to readers. The immutable source object provides provenance. The manifest authorizes stored resources and records their hashes, media types, browser roles, source URLs, capture diagnostics, and source-to-document relationship.

The snapshot entity does not store a JavaScript or interaction version. Executable delivery is not a second snapshot state. The current manifest contract and the browser security boundary are sufficient to decide how the canonical document may be served.

## Capture and normalization

The worker preserves scripts and inline event handlers while rewriting captured dependencies to snapshot-local paths.

For URL captures it retains the final successful main-document response before application scripts mutate the page. That response becomes `source.html` and is the input for the canonical document. If that response cannot be retained or parsed, the bounded rewritten browser document remains usable and the manifest records the diagnostic.

For HTML, MHTML, and WebArchive uploads, the uploaded bytes remain immutable source material. The importer decodes the archive, builds a deterministic resource map, produces the canonical document, and keeps JavaScript disabled only in the worker context used to generate the screenshot.

Normalization performs the following operations:

1. Remove source-provided CSP, refresh, referrer, integrity, credentialed CORS, ping, preconnect, prefetch, and prerender controls that conflict with archive isolation.
2. Rewrite captured document, stylesheet, image, font, media, frame, script, module, JSON, model, and binary references to safe relative snapshot paths.
3. Rewrite bounded JavaScript literal forms, including static module imports, dynamic imports, `new URL(..., import.meta.url)`, `fetch`, and XHR reads, when their absolute target is an exact captured resource.
4. Insert a deterministic `<base>` for the stored document location.
5. Insert a fixed snapshot shim before archived scripts. The shim resolves exact captured runtime URLs, forces credential omission, restricts methods to `GET` and `HEAD`, and disables selected connection capabilities as defense in depth.
6. Assign every served object one browser role: `root-document`, `nested-document`, or `asset`.
7. Hash the source, canonical document, and every manifest-authorized resource with SHA-256.
8. Record missing resources and normalization diagnostics without discarding an otherwise readable snapshot.

Normalization failures do not create a distinct static snapshot category. The worker retains the safest rewritten document it can produce, and the isolated response boundary continues to restrict its execution and network access.

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
    "normalizer": "archive-snapshot-v1",
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
    "MinimumChromiumMajorVersion": 152,
    "AllowedBrands": ["Chromium"],
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

Owner and administrator authorization continues through the authenticated API origin. After a private detail request succeeds, the API issues a short-lived data-protected bearer capability bound to exactly one snapshot and the authorized account.

The returned `contentUrl` places the capability in the content path:

```text
https://snapshots.example.net/api/snapshots/{id}/content/~{capability}/index.html
```

Relative document and asset URLs retain the same capability prefix. The snapshot origin validates the capability without cookies, then revalidates the bound account against the snapshot's current moderation state, owner, registration state, role, and permission overrides before every resource read. Invalid, expired, modified, snapshot-mismatched, or no-longer-authorized capabilities return `404`.

The capability lifetime defaults to 60 minutes and is configurable. Metadata responses containing it remain private and non-cacheable. Reloading an authorized snapshot page obtains a new capability.

Public snapshots keep the ordinary path without a capability:

```text
https://snapshots.example.net/api/snapshots/{id}/content/index.html
```

Thus moderation affects authorization only. Once access has been established, every completed snapshot uses the same JavaScript-capable content policy.

## Browser execution policy

The frontend embeds every snapshot with:

```html
<iframe sandbox="allow-scripts"></iframe>
```

`allow-same-origin` is intentionally absent. The document receives an opaque origin and cannot read application cookies or storage even if it is opened from an authorized private view.

The document response supplies a restrictive policy whose sources are scoped to the exact public or capability-prefixed snapshot content path. It permits captured scripts, inline historical scripts, styles, images, fonts, media, frames, and same-snapshot reads while denying objects, forms, workers, and connections outside that path.

Additional response controls include:

- `X-Content-Type-Options: nosniff`;
- `Referrer-Policy: no-referrer`;
- a restrictive `Permissions-Policy`;
- credential-free CORS for opaque-origin module and resource reads;
- `Cache-Control: no-store` for documents;
- role-specific navigation checks for HTML, SVG, XML, and other active formats.

## Browser capability requirement

Content Security Policy does not cover every connection channel available to untrusted JavaScript. In particular, navigation, redirects, DNS-related behavior, WebRTC, and newer connection APIs require a broader browser-enforced boundary.

The API therefore sends `Connection-Allowlist` scoped to the current snapshot. Redirects and WebRTC remain blocked by default. JavaScript is enabled only when the structured `Sec-CH-UA` hint identifies a configured Chromium product at or above the tested minimum version. Missing, malformed, conflicting, unknown, or older hints receive the same snapshot with scripts disabled.

This client gate protects readers from archived code; it is not a snapshot classification or moderation rule. Clients without the required enforceable network boundary receive a script-disabled response policy.

## Public API

`SnapshotResponse` keeps its existing shape. `contentUrl` remains the single entry URL and is null until capture storage completes.

- Public metadata returns a public snapshot-origin URL.
- Authorized owner and administrator metadata returns a short-lived capability URL on the same origin.
- The frontend does not select between different snapshot representations.

The OpenAPI description documents these semantics, and generated frontend types continue to be produced through `bun --bun run generate:api` while the backend is running.

## Deployment requirements

Deploy the API, worker, frontend, isolated snapshot-origin routing, and persistent API data-protection key ring together. Verify both public and access-controlled snapshot URLs through the configured credential-free origin. The reverse proxy must preserve the snapshot host and forward it to the API container without adding authentication or session cookies. See [Deployment](deployment.md) for the complete origin and proxy contract.
