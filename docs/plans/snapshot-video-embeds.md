# Plan: Preserve embedded media in snapshots

**Status:** Revised after repository analysis and Wayback/Webrecorder research; awaiting approval

**Prepared:** 2026-10-29

**Scope:** Reliable capture and isolated replay of finite, directly addressable HTML video and audio resources, including supporting poster and text-track files, media in captured frames, missing direct media in uploaded MHTML, and HTTP byte-range delivery. Adaptive, live, protected, generated, and legacy plugin media are classified explicitly but are not all promoted to guaranteed support.

## 1. Goal and acceptance boundary

The implementation should make an embedded media resource work offline when all bytes needed for replay can be captured as finite HTTP resources within the existing time, resource-count, and storage limits. Replay must never contact the original site or CDN.

The primary acceptance example is the supplied Fortnite MHTML. A successful import must store the referenced MP4 as an immutable snapshot asset, rewrite the `<video>` element to that local asset, and replay it in current Safari and Chromium with the external network disabled.

This task does not attempt to make every commercial streaming platform independently archivable. A snapshot that depends on uncaptured segments, a live source, a DRM licence, WebRTC, or a platform-specific extraction workflow must remain best-effort or incomplete rather than silently using live content.

## 2. Repository findings

### 2.1 Supplied MHTML

`C:\Users\christian\Downloads\Overdrive - Fortnite Emote - Fortnite.GG.mhtml` is 107,200 bytes. Its root document contains the quoted-printable equivalent of:

```html
<video class="item-video" loop autoplay playsinline
  data-id="3906" data-v="4"
  src="https://fnggcdn.com/items/3906/video.mp4?4"></video>
```

The archive contains HTML, CSS, JPEG, and WebP parts, but no `video/*` MIME part. `MhtmlDecoder` can only expose parts that are physically present, and `OfflinePageNormalizer` can only map those decoded resources. The live MP4 URL therefore remains unresolved and is correctly blocked by replay isolation.

### 2.2 URL capture

`CapturedResourceCollector` already accepts `audio/*` and `video/*`, but only after the browser requests them. `BrowserCaptureEngine.LoadDynamicContentAsync` scrolls the main document and targets lazy images and generic `data-*` elements; it does not deliberately load or play media and does not run a media behavior in child frames.

The collector accepts every successful 2xx response, including `206 Partial Content`, and stores only URL, content type, and response bytes. It drops `Content-Range`, so a small byte fragment can currently be published under the URL of the complete media object.

The collector also omits common adaptive-playlist media types such as `application/vnd.apple.mpegurl`. Merely adding those MIME types would not provide complete HLS/DASH support because playlists and their segment URL graphs would also need deterministic rewriting and completeness rules.

### 2.3 Canonical rewriting and runtime mapping

`SnapshotDocumentNormalizer` already rewrites mapped `src`, `href`, and `poster` attributes and `srcset`. Static `<video>`, `<audio>`, `<source>`, `<track>`, `<iframe>`, and `<embed>` `src` attributes therefore become local when their resource URL is present in the map.

There are narrower dynamic gaps in `SnapshotRuntimeShim`:

- dynamic `HTMLMediaElement.src`, `HTMLSourceElement.src`, and `HTMLVideoElement.poster` assignments are guarded and remapped;
- dynamic `HTMLTrackElement.src` is not currently guarded or remapped;
- `<object data>` is neither rewritten nor permitted by replay policy;
- `blob:` and `data:` media are allowed only when archived scripts reconstruct them from already available bytes.

### 2.4 Replay policy and ranges

The snapshot CSP already permits snapshot-local, `data:`, and `blob:` media through `media-src`. It permits only snapshot-local frames and deliberately keeps `object-src 'none'`.

`Permissions-Policy` currently contains `autoplay=()`, and the viewer iframe does not delegate autoplay. This explicitly disables an original media behavior even when the asset was captured successfully. DRM, camera, microphone, WebRTC, and other live capabilities are separately denied and must remain denied.

The API enables ASP.NET range handling only on the API-origin path. Snapshot-origin replay explicitly disables it. The S3 response stream is not guaranteed to be seekable, so the existing `enableRangeProcessing: true` is not a dependable cross-origin or cross-browser range implementation.

`IArchiveObjectStore` already returns content type and total length for a full read. It does not provide metadata-only or bounded range reads. `SnapshotResource` already contains a SHA-256 digest that can serve as a stable validator for the immutable representation.

## 3. Wayback/Webrecorder research

The relevant systems separate capture behavior from stored-resource identity:

1. Browsertrix runs **Autoplay** to find and start audio/video in the page and every iframe. **Autofetch** discovers dormant `data-*`, responsive-image, and lazy resource URLs. Its crawler enables autoplay, autofetch, autoscroll, and site-specific behavior by default. See [Browsertrix browser behaviors](https://crawler.docs.browsertrix.com/user-guide/behaviors/), the [Webrecorder behavior implementation](https://github.com/webrecorder/browsertrix-behaviors), and the [Browsertrix crawler defaults](https://github.com/webrecorder/browsertrix-crawler/blob/main/src/util/argParser.ts).
2. Wayback-style storage records retrieved HTTP resources and their protocol metadata rather than assigning a permanent HTML-element class to every object. WARC intentionally supports arbitrary binary resources while requiring only minimal knowledge of their nature. See [WARC 1.1](https://iipc.github.io/warc-specifications/specifications/warc-format/warc-1.1/).
3. pywb treats byte ranges as protocol semantics. A range starting at zero can be removed for the upstream recording request so a complete response is stored; replay can then synthesize a correct bounded `206` from the archived `200`. See [pywb range handling](https://github.com/webrecorder/pywb/blob/main/pywb/apps/rewriterapp.py) and [RFC 9110 range semantics](https://www.rfc-editor.org/rfc/rfc9110.html#name-range-requests).
4. Archive-It documents platform-specific capture and replay rather than claiming generic streaming support. YouTube capture is more reliable when a dedicated extractor runs, and streaming services can require custom scope or collection logic. See [Archive-It YouTube guidance](https://support.archive-it.org/hc/en-us/articles/208333753-Archiving-YouTube-videos) and [streaming-media troubleshooting](https://support.archive-it.org/hc/en-us/articles/10564990890900-Troubleshooting-streaming-downloadable-media).
5. Missing resources in Wayback mean that they were not archived; Wayback may fall back to a nearby capture or the live web. Archive must not copy that fallback because its replay security model promises a self-contained snapshot. See [Internet Archive's Wayback explanation](https://archivesupport.zendesk.com/hc/en-us/articles/360004651732-Using-The-Wayback-Machine).

The project should therefore adopt the capture/store/range pattern, but keep its stricter no-live-fallback replay boundary.

## 4. Media and embed classification

Classification is useful for choosing a capture strategy and defining support, but it should not become a new security role in the persisted manifest.

| Class | Examples | Proposed treatment |
| --- | --- | --- |
| Finite native media | `<video src>`, `<audio src>`, child `<source src>` with MP4, WebM, Ogg, MP3, AAC, WAV | Guaranteed when a complete 200 representation fits the limits; store as an ordinary `asset` and support byte ranges |
| Media support assets | `<video poster>`, `<track src>`/WebVTT | Capture/rewrite with the parent media; poster remains an image asset and track remains a text asset |
| Nested player document | `<iframe src>` containing native media or a script player | Preserve as `nested-document`; run media activation in the frame and retain only requests actually captured |
| Adaptive on-demand media | HLS/DASH manifests, segments, MSE-created `blob:` URLs | Best-effort only in this task; observed resources may be retained, but complete support requires playlist/segment-graph rewriting and finite-VOD completeness validation |
| Platform-specific player | YouTube, Vimeo, TikTok and comparable iframe/script players | Best-effort captured frame only; no yt-dlp/site extractor dependency in this task |
| Generated media | `data:` URLs, canvas output, `blob:` assembled entirely from captured bytes | Continue to work when existing archived scripts can reproduce it; no separate fetch is possible for an in-memory URL |
| Live or device media | live HLS/DASH, WebRTC, `MediaStream`, camera/microphone | Unsupported for self-contained replay; do not keep a capture open indefinitely and do not enable device/network capabilities |
| Encrypted media | EME/DRM and licence-bound streams | Unsupported; retain `encrypted-media=()` and never archive or replay licence sessions |
| Legacy active embed | `<object>`, `<embed>`, browser plugins/Flash | Unsupported as active content; retain `object-src 'none'` |
| Non-media embedded assets | images/`picture`, SVG, fonts, models, canvas dependencies | Existing asset capture remains authoritative; no new media classification is needed |

### Classification decision

Do not add `video`, `audio`, or `embed` values to `SnapshotResourceRoles`. That role is an authorization boundary describing whether an object may act as a root document, nested document, or inert asset. MIME type already distinguishes stored media for delivery and reporting.

Use a small internal capture-only candidate kind to route discovery and validation, for example `NativeMedia`, `Poster`, `TextTrack`, `NestedFrame`, and `AdaptiveManifest`. Do not persist that enum or increment the manifest version in this task. Persisted resources continue to use source URL, content type, hash, and the existing role. Missing finite media URLs continue to contribute to `MissingUrls` and `Incomplete`.

This keeps the manifest compatible and follows the WARC/Wayback separation between the context that triggered capture and the identity of the stored HTTP response.

## 5. Proposed implementation

### 5.1 Bounded media activation for URL captures

Add a focused media-capture behavior invoked after normal dynamic loading and before the collector is stopped:

- execute it in the main frame and every attached HTTP(S) child frame, skipping inaccessible/detached and `about:blank` frames;
- inspect `video` and `audio`, selected `currentSrc`, child `source[src]`, `track[src]`, `video[poster]`, and targeted media `data-src`/lazy attributes;
- scroll dormant elements into view, call `load()`, temporarily mute for capture activation, and attempt bounded `play()`/`pause()` only to trigger source selection and initial requests;
- restore transient media state and never serialize the temporary muted/playing state into the canonical document;
- repeat once for media inserted by mutations during the behavior window, deduplicate absolute HTTP(S) URLs, and stop within the existing job deadline;
- retain the current public-network guard, manual redirect checks, resource limit, and aggregate storage limit.

Do not import the Browsertrix behavior package. A local, narrow Playwright behavior avoids a new runtime dependency and keeps Archive's existing network boundary authoritative.

### 5.2 Store only complete media representations

Harden `CapturePageAccess` and `CapturedResourceCollector` around response semantics:

- preserve status, `Content-Length`, `Accept-Ranges`, and parsed `Content-Range` long enough to validate a response;
- for a single browser media range beginning at byte zero, perform the guarded route fetch without `Range`, following the same manually validated redirect rules, so the archive can receive a complete 200 representation;
- continue to satisfy the browser from that response; a normal 200 is the only representation committed under the full URL;
- reject a lone 206, a non-zero-only range, mismatched body length, unknown total length, malformed range metadata, or a server that still returns an incomplete representation;
- reject media whose declared full length exceeds remaining snapshot capacity before reading the body;
- bound the media response-body wait so an infinite/chunked live response becomes a failed media resource instead of consuming the entire capture deadline;
- allow a later complete response for the same URL to supersede an earlier failed partial attempt using the collector's existing request sequence rules.

No partial assembly is planned. Joining multiple disjoint responses safely would require validators and proof that every segment belongs to the same representation.

### 5.3 Bounded completion of uploaded MHTML

MHTML cannot contain bytes that were not saved by the exporting browser. To make the supplied fixture playable, add an explicit ingestion-time completion pass after decoding and before offline normalization:

- recursively inspect the root and included frame documents for missing direct `video[src]`, `audio[src]`, media `source[src]`, `track[src]`, and `video[poster]` resources;
- resolve candidates against each document's declared public HTTP(S) base URL and ignore unresolvable, `data:`, `blob:`, `file:`, and other non-HTTP schemes;
- fetch only those explicit media/support candidates, not arbitrary page dependencies or missing external iframe documents;
- use a credential-free/referrer-free HTTP client, manual redirects, and `PublicNetworkGuard` validation on every hop;
- request a complete representation without `Range`; accept only a complete 200 with an allowed audio/video, `text/vtt`, or image content type;
- use response-header length checks plus a capped streaming copy so the body cannot exceed remaining aggregate storage, resource count, or job time limits;
- add successful responses as ordinary `ImportedResource` instances so existing path allocation, hashing, canonical rewriting, manifest authorization, and immutable object storage remain responsible for the final artifact;
- record a failed, oversized, partial, redirected-to-private, or timed-out finite candidate as missing and mark the snapshot incomplete.

The original uploaded MHTML stays byte-for-byte unchanged as provenance. Completion is an ingestion operation only; replay never reaches the original CDN.

### 5.4 Canonical and dynamic URL handling

Keep the existing generic static rewrite for `src`, `poster`, and frame URLs. Add only the missing dynamic guards needed by the supported set:

- add `HTMLTrackElement.src` and `TRACK[src]` to `SnapshotRuntimeShim` URL remapping;
- retain `HTMLSourceElement.src`, `HTMLMediaElement.src`, and `HTMLVideoElement.poster` handling;
- leave `<object data>` and plugin activation blocked;
- do not rewrite HLS/DASH playlist bodies in this task;
- preserve `autoplay`, `loop`, `muted`, `playsinline`, `preload`, and original controls.

For a successfully mapped direct media element that has no original controls, add native controls only as an offline-replay fallback when autoplay cannot start. Implement this as a replay-time fallback after a rejected autoplay attempt rather than an unconditional canonical markup change. This keeps working custom players visually unchanged while making the supplied script-incomplete MHTML operable under normal browser autoplay restrictions.

### 5.5 Explicit immutable byte-range delivery

Implement ranges explicitly instead of relying on `File(..., enableRangeProcessing: true)` over an object-store stream:

- extend `IArchiveObjectStore` with metadata-only and bounded single-range reads; implement S3 reads with a single `Range` request and update all test stores;
- authorize snapshot visibility/capability, normalized path, manifest membership, object-key prefix, resource role, and fetch destination before opening any range;
- expose ranges for immutable manifest assets, including direct media and PDF/frame assets, but not for runtime-upgraded HTML documents whose served bytes may differ from stored bytes;
- no `Range`: return `200`, the full length, `Accept-Ranges: bytes`, and a strong ETag derived from the manifest SHA-256;
- one satisfiable closed, open-ended, or suffix range: return `206` with exact `Content-Range`, `Content-Length`, `Accept-Ranges`, and the same ETag;
- malformed, multiple, or unsatisfiable ranges: return `416` with `Content-Range: bytes */<total>`;
- honor `If-Range` against the immutable SHA-256 validator by returning the full 200 representation when it does not match;
- apply identical semantics to public and short-lived private-capability paths without weakening their authorization ordering.

Amazon S3 supports one byte range per GET, which matches this design. Multiple multipart ranges are intentionally out of scope. See [S3 `GetObject` range behavior](https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetObject.html).

### 5.6 Replay security and autoplay fidelity

Keep the current snapshot-origin isolation:

- retain snapshot-local-only `media-src`, `frame-src`, and `connect-src` plus allowed in-memory `data:`/`blob:` sources;
- retain `object-src 'none'`, no live fallback, credential omission, and blocked WebSocket/WebTransport/WebRTC paths;
- retain `encrypted-media=()`, `camera=()`, and `microphone=()`;
- remove the blanket `autoplay=()` denial and delegate only `autoplay` to the snapshot viewer iframe so the archived document can exercise its original autoplay behavior subject to the browser's own user-gesture and audible-media policy.

The outer viewer remains sandboxed with `allow-scripts` and without `allow-same-origin`. Autoplay delegation does not authorize network access or DRM. The [Permissions Policy specification](https://www.w3.org/TR/permissions-policy/) confirms that an empty allowlist disables a feature for the document and its descendants; the [HTML media specification](https://html.spec.whatwg.org/multipage/media.html) defines native sources, tracks, autoplay, controls, loading, and seeking behavior.

### 5.7 Adaptive and platform media boundary

Do not advertise complete HLS, DASH, MSE, YouTube, Vimeo, TikTok, or live-stream support in this task.

The worker may retain finite manifest/segment responses that the original player actually requests and that already pass the generic capture rules. However, full adaptive support requires a later design that:

- distinguishes finite VOD from live/event playlists;
- parses and rewrites every manifest-level URL relative to the manifest's original URL;
- selects or captures a bounded representation graph;
- validates that every required initialization, media, key, and alternate-track segment is present;
- defines capture quality for partially collected variants;
- handles native Safari HLS separately from JavaScript/MSE players.

DRM remains excluded because encrypted replay depends on key-system and licence exchange, and WebRTC/device streams remain excluded because they are live capabilities rather than finite HTTP representations. See [Encrypted Media Extensions](https://www.w3.org/TR/encrypted-media/) and [Media Capture and Streams](https://www.w3.org/TR/mediacapture-streams/).

## 6. Expected code and documentation changes

### Worker capture

- `BrowserCaptureEngine`: invoke frame-aware, bounded media activation.
- New focused media-candidate/activation helper: DOM discovery and transient playback behavior.
- `CapturePageAccess` and `BrowserRequestRouter`: guarded start-at-zero range normalization.
- `CapturedResourceCollector`/archive tests: response completeness, media timeouts, and partial-response rejection.

### Offline import

- `OfflineImportEngine` and a focused guarded media-completion service.
- `ImportedPage` traversal and synthetic MHTML fixtures covering direct video, audio, source, track, poster, nested frame, failures, and immutable provenance.
- Worker dependency registration for the credential-free completion client without changing unrelated import formats.

### Canonical replay

- `SnapshotRuntimeShim`: dynamic text-track mapping and conditional native-control fallback.
- `SnapshotContentPolicy` and the existing snapshot viewer iframe: narrowly restore autoplay delegation while retaining every unrelated capability denial.
- Normalizer/runtime tests; no new route or independent UI view is introduced, so no HTML mockup is required.

### Storage and API

- `IArchiveObjectStore`, `S3ArchiveObjectStore`, and in-memory test implementations: metadata and single-range reads.
- Snapshot content repository/result contract: immutable length, digest/ETag, and range-capable asset opening after manifest authorization.
- `SnapshotsController` or a focused range result/parser: deterministic 200/206/416 handling for both origins and private capability URLs.

### Documentation

Update `docs/interactive-snapshots.md`, configuration/deployment notes if a new bounded media timeout is exposed, and endpoint documentation. Document direct-media support, incomplete behavior, codec dependence, adaptive/live/DRM exclusions, and byte-range requirements.

## 7. Validation strategy

### Automated worker/import tests

- direct video and audio that request bytes only after `load()`/`play()`;
- media in a nested iframe and media inserted during the bounded mutation window;
- child `<source>`, `currentSrc`, poster, WebVTT track, targeted lazy attributes, and deduplication;
- complete 200 capture; start-at-zero browser range normalized to a complete 200; lone/non-zero/invalid 206 rejected; later complete response superseding an earlier partial failure;
- declared oversize, streamed oversize, timeout/infinite response, redirect limit, and private/reserved network targets;
- a compact synthetic equivalent of the supplied Fortnite MHTML plus an optional manual import of the real file;
- original upload hash unchanged and snapshot marked incomplete when direct media completion fails;
- adaptive, live, DRM, `object`, and device-media cases remain outside the guaranteed path.

### Automated replay/API tests

- static and dynamic media, source, poster, and track URL rewriting;
- no request can escape to the original CDN;
- autoplay is delegated while encrypted media, camera, microphone, WebRTC, object/plugin, and external frame access remain denied;
- native controls appear only after failed autoplay on a mapped direct element without original controls;
- full 200, closed/open/suffix 206, exact headers, invalid/multiple/unsatisfiable 416, `If-Range`, cancellation, and non-seekable S3-style streams;
- authorization and manifest validation occur before metadata/range reads for public, owner, and privileged private access.

### Manual acceptance

1. Import the supplied Fortnite MHTML in a network-enabled worker environment.
2. Confirm the MP4 is stored and manifest-authorized, while the original MHTML object and hash are unchanged.
3. Disable external network access before replay and confirm that no request reaches `fnggcdn.com`.
4. Verify load, autoplay/fallback controls, play/pause, loop, seeking, reload, and text-track/poster behavior in current Safari and Chromium.
5. Repeat through a private capability URL and confirm identical media/range behavior.
6. Run the standard backend build/tests and frontend checks; visually verify the existing viewer at desktop and mobile sizes because its iframe permission attribute changes, even though layout does not.

## 8. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Large or infinite media exhausts memory/time | Check declared length, cap streamed import copies, bound browser media-body waits, and enforce existing aggregate limits |
| A 206 fragment is mistaken for a full file | Store only validated complete 200 representations; never assemble or publish a lone partial body |
| MHTML completion introduces SSRF | Explicit DOM candidates only, HTTP(S) only, `PublicNetworkGuard` on every hop, manual redirects, no credentials/referrer |
| Autoplay produces unexpected sound | Delegate only autoplay and retain normal browser autoplay/user-gesture rules; never relax media/network/DRM isolation |
| Conditional fallback controls alter appearance | Add them only after rejected autoplay on successfully mapped media lacking original controls |
| Browser cannot decode captured codec | Preserve original bytes/MIME type; do not transcode; document codec support as a client precondition |
| Adaptive capture appears more complete than it is | Do not claim adaptive support; leave playlist graph capture/rewrite and quality semantics to a separate approved task |
| Range support leaks private content | Perform visibility/capability, manifest, path, role, and destination authorization before any object metadata or range read |

## 9. Version-control execution after approval

Before implementation:

1. fetch the remote repository;
2. verify that local `master` can be fast-forwarded without overwriting local work;
3. update `master` and create `codex/snapshot-embedded-media` from it;
4. leave unrelated plan files and prior branch changes untouched.

Proposed commits:

- `feat: capture finite snapshot media`
- `feat: complete missing mhtml media`
- `fix: serve ranged snapshot assets`
- `docs: document embedded media support`

## 10. Approval record

Implementation has not started. Explicit approval is still required under `AGENTS.md`.

Approval of this plan specifically authorizes:

- bounded, credential-free ingestion-time retrieval of explicit missing media/support URLs from uploaded MHTML;
- temporary capture-time media activation in every captured frame;
- explicit single-range delivery of immutable snapshot assets;
- narrowly restoring autoplay delegation while retaining the remaining replay isolation controls.

Without ingestion-time retrieval approval, the supplied MHTML must remain video-incomplete because the MP4 bytes are not present in the uploaded file.
