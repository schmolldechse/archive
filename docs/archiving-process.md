# How archiving works

This document defines what an archive.org snapshot means, how the current implementation creates one, and which guarantees it does and does not provide.

The format rules and adapter design are described in [File imports](file-imports.md).

## Preservation model

A live web page is not a single file. It is a time-dependent execution involving HTML, stylesheets, scripts, fonts, media, APIs, browser state, network timing, consent state, and server-side personalization. Capturing it requires choosing an observation point and deciding which parts of that execution become evidence.

archive.org currently uses a rendered-snapshot model:

- Chromium observes the page at a defined viewport and locale.
- The worker waits for initial loading and then exercises vertical scrolling to trigger lazy content.
- The current DOM, a viewport screenshot, and selected successful response bodies are retained.
- References to retained resources are rewritten to archive URLs.
- Descriptive and operational metadata records when and how the capture occurred.

This model aims for a useful replayable representation. It is not a packet trace, a complete browser profile, a server-side record, or a proof that the origin served identical bytes to another observer.

## Timestamp semantics

The public snapshot `createdAt` value is the time at which the API accepted the archive job. The database also stores worker start and completion times. Duration is measured by the worker from the beginning of capture-engine execution through object creation.

The timestamp therefore establishes the system's workflow time, not an externally notarized publication time. There is currently no trusted timestamp authority.

## URL capture lifecycle

### 1. Validate and normalize the request

The API accepts only absolute HTTP or HTTPS URLs. It:

- trims the submitted values;
- lowercases scheme and host;
- removes URL fragments;
- removes a trailing slash from non-root paths;
- preserves the query string;
- validates title, description, upload size, tag count, and tag length;
- normalizes tags to lowercase and removes duplicates.

The normalized URL becomes the durable source identity for the job.

### 2. Enqueue durable work

The API creates an `ArchiveJob` in PostgreSQL with:

- status `QUEUED`;
- progress step `SOURCE_CHECK`;
- user-supplied descriptive metadata;
- normalized tags;
- a project grouping key.

The request returns a job ID immediately. Clients can poll the job or receive changed state through server-sent events.

### 3. Claim the job

The worker that owns the PostgreSQL advisory lease selects the oldest queued job in a transaction. Row-level locking with `FOR UPDATE SKIP LOCKED` prevents duplicate ownership even if queue concurrency is introduced later.

The job becomes `RUNNING` and receives a start timestamp before network capture begins.

### 4. Enforce crawl policy before navigation

If crawl-policy enforcement is enabled, the worker fetches `/robots.txt` from the target origin before opening the page.

The policy implementation:

- allows up to three redirect hops for `robots.txt`;
- validates every policy URL as a public network destination;
- sends the configured archive User-Agent;
- adds Web Bot Auth headers when signing is enabled;
- treats `404` and `410` as allow-all;
- treats `401` and `403` as disallow-all;
- fails closed for other unsuccessful or ambiguous responses;
- limits the maximum policy-file size;
- chooses the most specific matching user-agent group;
- applies longest-match allow/disallow rules;
- honors a valid `crawl-delay` up to 300 seconds;
- combines that delay with the configured minimum per-origin interval.

The in-memory host limiter is scoped to the active worker process. The single-worker lease makes that sufficient for the current topology.

### 5. Establish safe page access

Every browser request passes through a context-level router. Only pages explicitly registered by the capture engine may access the network; popups and unknown pages are denied.

For each request the worker:

- accepts only HTTP(S);
- resolves hostnames and rejects loopback, private, link-local, multicast, documentation, benchmark, and other non-public address ranges;
- blocks WebSockets;
- fetches one redirect hop at a time so the next target is checked again;
- rejects unexpected main-frame navigation;
- optionally adds short-lived Ed25519 Web Bot Auth signatures.

These checks reduce SSRF risk but do not close DNS-rebinding or proxy-level gaps. A production network policy must independently restrict egress.

### 6. Separate access from capture

The first page exists only to establish access. This is particularly important for Cloudflare:

1. The worker detects challenge response headers, known challenge paths, and challenge-like documents.
2. It waits only for automatic access within the configured timeout.
3. It does not solve CAPTCHAs or simulate a human identity.
4. It closes the access page after approval or failure.
5. It opens a fresh page in the same browser context, retaining only context state such as cookies.

Late responses from the access phase therefore cannot leak into the recorded resource set.

A challenge response or challenge document is never accepted as archival content. If a challenge reappears during the clean attempt, the worker may retry according to `ChallengeReloadAttempts` and otherwise fails the job.

### 7. Load the clean document

The clean capture page navigates to the exact normalized source URL. The worker rejects:

- non-success main-document responses;
- unexpected main-frame redirects;
- document changes during the final capture window;
- challenge content;
- disallowed network targets.

The browser context is fixed to a 1440 × 900 viewport, `de-DE` locale, the configured User-Agent, and blocked service workers.

### 8. Exercise dynamic content

After initial loading, the worker:

- waits briefly for network idle, but tolerates analytics or streaming connections that never become idle;
- installs a DOM mutation observer;
- scrolls downward by roughly 90% of the viewport;
- pauses for the configured quiet period after each round;
- stops when it reaches a stable page bottom or the scroll-round limit;
- brings common lazy-loaded elements into view;
- returns to the top and waits once more.

This is deterministic enough to reproduce the current behavior, but it cannot guarantee that all application states, infinite feeds, interactions, or delayed resources have been exercised.

### 9. Collect eligible resources

The response collector observes successful non-main-document responses. It stores:

- `text/*`;
- JavaScript;
- JSON;
- images;
- fonts;
- audio;
- video.

It ignores empty responses, HEAD requests, non-success status codes, unsupported media types, and Cloudflare challenge resources.

Resources are keyed by their absolute response URL. A later successful retry clears an earlier failure for the same URL. A later failure after a success marks the result incomplete. Resource count and byte limits are enforced while bodies are collected.

### 10. Freeze the observation

Once dynamic loading is complete, the worker:

- serializes a clone of the current document;
- removes known injected Cloudflare challenge resources from that clone;
- captures a PNG of the configured viewport;
- drains the resource collector;
- rechecks the current document and navigation version;
- evaluates `X-Robots-Tag` and matching HTML robots meta directives;
- rejects `noarchive` and `none`;
- closes the page before writing objects.

Closing the page establishes a boundary: later navigation or network activity cannot alter the validated HTML, screenshot, or drained resource collection.

### 11. Rewrite and store

For each captured resource URL, the worker creates an archive asset path using SHA-256 over the URL string. It rewrites:

- direct absolute references present in the serialized HTML;
- escaped ampersand variants;
- resolvable `src`, `href`, and `poster` attributes;
- quoted and unquoted CSS `url(...)` references;
- references inside captured CSS.

A local `<base>` element points relative references at the snapshot content endpoint.

Objects are written in this order:

1. rewritten `index.html`;
2. `screenshot.png`;
3. captured assets;
4. `manifest.json`.

If any write or limit check fails, objects already written for that attempt are deleted in reverse order.

### 12. Publish atomically in PostgreSQL

After object creation, the worker starts a database transaction and locks the job row. It publishes only if the job is still `RUNNING`.

The transaction:

- finds or creates the project;
- creates a snapshot with the same GUID as the job;
- normalizes reusable tags and snapshot-tag relationships;
- records quality, timings, resource count, storage size, and object prefix;
- marks the job `COMPLETED` with 100% progress.

Readers do not see a snapshot row until this transaction commits.

## Uploaded file lifecycle

The API accepts non-empty `.html`, `.mhtml`, and `.webarchive` files through the existing multipart form. It checks the extension and a bounded content prefix, applies `Archive:MaxUploadBytes`, stores `jobs/<job-id>/source.<format>`, and persists `HTML`, `MHTML`, or `WEBARCHIVE` as the job source type. A client-supplied MIME type does not choose the decoder.

The worker dispatches the file to its registered decoder. The MHTML decoder selects the `multipart/related` root and decodes MIME parts. The project-owned Webarchive adapter reads Apple's binary or XML property list and extracts the main HTML, subresources, and subframes. Both produce the same intermediate document as the HTML decoder. The normalizer assigns local paths to embedded assets and frames and rewrites HTML/CSS references.

Chromium renders the normalized document at 1440 × 900 with JavaScript disabled. Its request router fulfills only assets contained in the uploaded package and aborts all other requests. The worker stores `index.html`, assets and frames, `screenshot.png`, the original `source.<format>`, and manifest version 2. That manifest records source size and SHA-256, asset keys, and missing request URLs. The database snapshot is published only after storage succeeds. The temporary upload is then removed.

Imported HTML is served with a restrictive Content Security Policy; original sources are delivered as downloads. A missing rendered resource sets quality to `INCOMPLETE`. The screenshot records the Chromium replay result, which can differ from Safari or Chrome at the original save time. The frontend links from the selected snapshot to the archived HTML and shows its screenshot as a preview.

## Completeness and quality

For URL capture, `COMPLETE` means that the collector had no unresolved monitored resource failures when capture stopped. For file imports, it means the offline renderer requested no resources absent from the package. `INCOMPLETE` records unresolved requests in either path.

This quality flag does not mean:

- every page resource was eligible for collection;
- every interactive state was explored;
- all third-party or authenticated content was visible;
- the page will replay pixel-perfectly;
- the screenshot covers the full scroll height;
- the stored bytes have been cryptographically verified.

The UI should present quality as capture completeness, not legal authenticity or cryptographic integrity.

## Limits and terminal outcomes

The default limits are:

| Limit | Default |
| --- | ---: |
| Capture duration | 120 seconds |
| Stored bytes per snapshot | 250,000,000 |
| Captured resources | 500 |
| Dynamic scroll rounds | 24 |
| Quiet period | 1,000 milliseconds |
| Uploaded file size | 250,000,000 bytes |

Exceeding duration, storage, or resource limits produces `DISCARDED`. Explicit cancellation produces `CANCELLED`. Access, policy, browser, or storage errors produce `FAILED`. Work left running by a prior worker process becomes `ABORTED` after restart.

No snapshot row is published for these outcomes.

## Integrity, authenticity, and provenance

These concepts are distinct:

- **Completeness** asks whether the capture process observed unresolved failures.
- **Integrity** asks whether stored bytes have changed since capture.
- **Authenticity** asks whether the captured representation genuinely corresponds to the claimed source and time.
- **Provenance** records the process, software, configuration, and transformations that produced the representation.

The current system records useful provenance such as source URL, timestamps, browser behavior, counts, and a manifest. Import manifest version 2 hashes the original uploaded file. There are no per-asset content hashes, software-version attestations, response headers, TLS evidence, signed manifests, or external timestamps.

The SHA-256 asset filename hashes the original URL, not the resource bytes. It must not be interpreted as a fixity check.

## Replay behavior

The API serves `index.html` and assets from the stored object prefix. Rewritten references keep captured dependencies inside the archive where possible. References that were not captured may remain unresolved or may still refer to their original absolute URL.

URL-capture HTML remains active content: opening it can execute retained scripts and may attempt new requests. Imported HTML is served with a restrictive CSP that blocks scripts and connections, while its original source is offered only as a download. A public deployment should use a dedicated, origin-isolated content host for all archived HTML or an equivalent sandboxing design. The screenshot is the safer passive visual representation.

## Reproducing the current implementation

A compatible implementation must preserve these invariants:

1. Validate and normalize before durable enqueue.
2. Keep submission separate from untrusted browser access.
3. Enforce crawl policy before page navigation.
4. Check every HTTP redirect hop and deny non-public destinations.
5. Never accept a challenge page as the final document.
6. Separate access-establishment and clean-capture pages.
7. Drain resource callbacks before reading the final resource set.
8. Revalidate document identity and archive directives after capture.
9. Close the browser page before storage begins.
10. Write all objects before exposing snapshot metadata.
11. Publish snapshot metadata and job completion in one database transaction.
12. Delete unreferenced partial objects, but retain them if commit state is ambiguous.

The primary implementation entry point is `backend/Archive.Worker/Capture/BrowserCaptureEngine.cs`. Supporting boundaries are listed in [Architecture](architecture.md).

## Future preservation improvements

Potential improvements, in increasing order of semantic impact:

- store final response headers and capture software versions;
- compute SHA-256 or stronger content digests for every stored object;
- sign a canonical manifest and include an external trusted timestamp;
- record redirect and request/response provenance;
- serve replay from a dedicated untrusted-content origin with a restrictive CSP;
- export or additionally retain standards-based WARC records;
- add deterministic browser/container version pinning and capture-environment attestations;
- add scheduled recapture and policy-aware retention;
- reconcile orphaned objects against database metadata.

Each change should define whether it improves replay, fixity, authenticity, provenance, or operational reliability; those goals are related but not interchangeable.
