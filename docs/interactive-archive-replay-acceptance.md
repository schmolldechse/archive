# Interactive archive replay acceptance

This note records the local acceptance baseline for replay version 1 on 24 October 2026. It complements the design in [Interactive archive replay proposal](interactive-archive-replay-proposal.md).

## Environment

- API and frontend ran directly on Windows at `http://localhost:5200` and `http://localhost:5173`.
- Replay used the separate local origin `http://127.0.0.1:5200`.
- The worker, PostgreSQL 18, and MinIO ran in WSL under the existing `archive-wsl-local` Compose project. Container labels, mounts, ports, and volumes were inspected before reuse; no unrelated resources or project volumes were removed.
- Browser acceptance used the official Chrome for Testing Stable `156.0.8078.47`, above the configured minimum Chromium major version 152.
- The locally generated WebArchive and MHTML fixtures, browser harness, screenshots, OAuth values, and service credentials remained under the ignored `.secrets` directory.

## Complexity and capture results

The scores use the five dimensions from the proposal in this order: DOM, captured resources, JavaScript graph, interaction/state, and rich composition.

| Level | Snapshot | Source and format | Nodes | Resources | Score | Capture result |
| --- | --- | --- | ---: | ---: | --- | --- |
| Simple | `d8b35db6-6c32-40b0-bd15-8ae05d1dd99d` | CERN, `TheProject.html`, WebArchive | 54 | 0 | `0+0+0+0+0 = 0` | `COMPLETE`, replay 1, 87,187 bytes |
| Intermediate | `50d044d6-c81a-49e3-8fbc-64fa7bc72689` | MDN, `bouncing-balls`, MHTML | 12 | 2 | `0+0+1+2+2 = 5` | `COMPLETE`, replay 1, 17,851 bytes |
| Complex | `0afc8a31-174c-4d2b-b42c-a643ee343d05` | MDN, WebGL tutorial sample 4, URL | 34 | 4 | `0+0+2+2+2 = 6` | `COMPLETE`, replay 1, 92,687 bytes |

All captures completed at approximately 09:37 CEST. Their manifests reported no missing resources. The simple page contains no script. The intermediate page uses one classic script, a continuously animated 2D canvas, and no storage. The complex page uses a local ES-module graph, a captured gl-matrix dependency, continuous animation, and WebGL.

## Browser and security verification

- All three documents remained readable through the existing `/api/snapshots/{id}/content/{path}` endpoint on the replay origin; relative assets used the same route.
- The MHTML canvas advanced in both a 1440×1000 desktop viewport and a 390×844 mobile viewport. The canvas adapted to 1440×928 and 390×772 respectively, with no console or page errors and no external requests.
- The URL capture loaded all four JavaScript resources with successful responses. WebGL created a live program with no GL error and rendered the rotating vertex-colored square.
- Public frames used `sandbox="allow-scripts"` and `referrerpolicy="no-referrer"`; no `allow-same-origin` token was present.
- A navigation attempt from the replay frame to a separate local TCP sentinel produced no connection. Chrome replaced the frame with its blocked-navigation error document, confirming enforcement before network egress.
- Requests without an admitted `Sec-CH-UA` received the static CSP with `script-src 'none'`; admitted Chrome 156 requests received the interactive CSP and `Connection-Allowlist`. Both paths returned identical `index.html` bytes.
- The removed `/replay/snapshots/...` route returned `404`. The replay host rejected snapshot metadata and private API paths while accepting only the existing public content route and its read-only methods. The API-host form of that content route remained available as a static backward-compatible alias.
- The intermediate snapshot was temporarily marked with `replay_version = NULL` to represent a legacy capture. Its public `contentUrl` did not change, returned HTTP 200 with `script-src 'none'`, emitted no `Connection-Allowlist`, and was restored to replay version 1 after the check.

The visual review found no Archive chrome regressions at desktop or mobile sizes. The preserved pages retain their own historical styling, while the existing header, back action, and information control remain aligned with the application's editorial interface.
