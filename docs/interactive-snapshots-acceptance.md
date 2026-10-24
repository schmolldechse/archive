# Interactive snapshot acceptance

This note records the local browser and storage baseline verified on 24 October 2026. It complements [JavaScript interaction for archived snapshots](interactive-snapshots-proposal.md).

## Environment

- API: `http://localhost:5200`
- Isolated snapshot origin: `http://127.0.0.1:5200`
- Frontend: `http://localhost:5173`
- Browser: Chrome 156
- Storage: local PostgreSQL and MinIO services running through WSL

## Snapshot matrix

The selection uses the documented five-dimension complexity score.

| Class | Snapshot | Source | Format | Score | Result |
| --- | --- | --- | --- | ---: | --- |
| Simple | `d8b35db6-6c32-40b0-bd15-8ae05d1dd99d` | CERN, `TheProject.html` | WebArchive | 0 | complete and readable |
| Intermediate | `50d044d6-c81a-49e3-8fbc-64fa7bc72689` | MDN, `bouncing-balls` | MHTML | 5 | scripts and canvas interaction completed |
| Complex | `0afc8a31-174c-4d2b-b42c-a643ee343d05` | MDN WebGL tutorial sample 4 | URL | 6 | modules and WebGL completed |

## Browser results

- All documents loaded through `/api/snapshots/{id}/content/{path}` on the isolated snapshot origin.
- Relative documents, modules, images, fonts, and data resources remained within their snapshot paths.
- Captured JavaScript executed automatically in admitted Chrome without an activation control.
- Desktop and mobile viewport checks completed without layout regressions in Archive chrome.
- Static delivery remained readable when the required browser capability signal was omitted.
- Browser console and page-error probes reported no unexpected application errors.
- The complex URL fixture rendered WebGL output; the MHTML fixture executed its preserved animation and canvas behavior.

## Isolation results

- External Fetch, XHR, WebSocket, WebTransport, redirect, and WebRTC/STUN attempts produced no permitted external connection.
- A navigation attempt to a separate local sentinel was blocked before network egress.
- Forms, workers, pop-ups, downloads, storage access, device permissions, and top-level navigation remained unavailable.
- Snapshot responses contained no application credentials and the document retained an opaque origin.
- The snapshot origin rejected metadata, owner, administrator, authentication, and mutation routes.
- Requests without an allowed manifest resource and browser destination returned `404`.

## Storage preparation

- Initial manifest-v4 audit: all 33 stored generations were eligible for preparation.
- Bulk preparation completed successfully for all 33 generations without a failed activation.
- Original generations and rollback journals were retained for every prepared snapshot.
- Final read-only audit: 33 of 33 stored snapshots reported `Current`.

The original generations and journals remain intentionally available until production observation and cleanup are complete.
