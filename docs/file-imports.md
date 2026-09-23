# Importing saved web pages

This document describes the implemented file-import path. See [Architecture](architecture.md) for component boundaries, [How archiving works](archiving-process.md) for the full job lifecycle, and [Configuration](configuration.md) for limits and deployment settings.

## Submission and source identity

`POST /api/archive` accepts `multipart/form-data` with a required `file` and `title`, plus optional `description`, repeated `tags`, and `originalLink`. Supported extensions are `.html`, `.mhtml`, and `.webarchive`. The API checks the extension and the first 8 KiB of content before queuing a job; the client-provided MIME type does not select the format. The worker performs the full parse. Existing HTML submissions use the same endpoint.

The selected format is stored as `HTML`, `MHTML`, or `WEBARCHIVE` in the job and snapshot. A normalized `originalLink`, when supplied, is the snapshot's claimed source URL and project-grouping key. URLs declared inside the uploaded file are used to resolve relative references and are not proof of origin. They are not fetched during import. Without `originalLink`, the project key is specific to the upload job. URL capture and its crawl and Cloudflare policies remain separate from file import.

The API places the incoming file at `jobs/<job-id>/source.<format>`. This temporary key uses a canonical extension, never a client-supplied path. The source is copied into the published snapshot and the temporary object is removed after publication. Terminal failures also trigger cleanup when the worker can confirm the job state.

## Format adapters

The API registers an `IUploadFormatProbe` for each file type. The worker resolves exactly one `IUploadedDocumentDecoder` by the persisted source type. Each decoder returns an `ImportedPage` containing the main HTML, an optional base URL, embedded resources, and subframes. The shared normalizer, renderer, and storage path do not depend on the source format. Parsing belongs in the worker so HTTP submission remains short and the same job cancellation and publication rules apply to every upload.

| Format | Decoder behavior |
| --- | --- |
| HTML | Decodes the uploaded bytes as HTML without bundled resources. |
| MHTML | Uses MimeKit to parse `multipart/related`, selects the HTML root using MIME root information or the declared content location, and maps `Content-Location` and `Content-ID` references to embedded parts. Additional HTML parts become frames. A `chrome-error://` part is not selected as the main page. |
| Webarchive | Uses the project's own binary/XML property-list reader. It does not invoke `textutil` or depend on `WebArchiveExtractor`. `WebMainResource` supplies the main HTML; `WebSubresources` supplies assets; `WebSubframeArchives` supplies nested frames. |

The Webarchive main resource must contain nonempty `text/html` data. Resource entries retain their bytes and MIME types. `WebResourceURL` identifies assets; `WebResourceTextEncodingName`, when present, controls text decoding. For the main HTML, decoding falls back through a byte-order mark, an HTML charset declaration, and strict UTF-8. Identical resource URLs with different bytes in the same frame are rejected. Resources in separate frames have separate storage scopes. Optional Apple metadata, such as `WebResourceResponse`, is not required for import.

The property-list reader bounds file size, object count, and nesting depth. It checks binary trailers, offsets, references, and ranges, and rejects cycles. XML parsing prohibits DTDs and external entities. Unknown optional archive keys with supported property-list values are ignored; missing required structure fails the job. See [Configuration](configuration.md#capture-limits) for the configured bounds.

## Offline normalization and replay

The normalizer assigns stable local paths to contained assets and rewrites references in HTML and CSS, including `cid:` aliases, relative URLs, `srcset`, CSS `url(...)`, and `@import`. It stores subframe HTML separately. Frame documents are paired with iframe elements in document order; complex frame relationships may need a future mapping improvement. Original resource URLs are never used as object-store paths.

Chromium renders the normalized page at 1440 × 900 with the `de-DE` locale and JavaScript disabled. The renderer fulfills requests only for assets extracted from the upload and aborts all others. It records blocked request URLs in the manifest. The screenshot is a view of this offline replay, not evidence that Safari or the original site displayed identical pixels when the file was saved.

The API serves imported HTML with a restrictive Content Security Policy that blocks scripts and network connections. It serves the preserved original file with `Content-Disposition: attachment`. In the frontend, selecting a snapshot shows the screenshot and offers a link to its archived HTML. Missing resources may affect that HTML view; they are never filled by contacting the claimed source site.

## Stored objects and manifest

Successful imports write the following objects before the database publishes the snapshot:

```text
snapshots/<snapshot-id>/index.html
snapshots/<snapshot-id>/screenshot.png
snapshots/<snapshot-id>/assets/<scoped-resource-hash>
snapshots/<snapshot-id>/frames/<frame-id>.html
snapshots/<snapshot-id>/source.<html|mhtml|webarchive>
snapshots/<snapshot-id>/manifest.json
```

Import manifests use version 2 and record the source format, declared page URL, object keys, resource URLs and media types, original file size and SHA-256, and the number of unavailable requests. `missingUrls` lists at most the first 100 of those requests; `missingUrlCount` is the total. URL captures continue to use their existing version 1 manifest. The original-file hash provides a check for that file alone; asset key hashes identify source URLs and are not content-fixity hashes.

An import is `COMPLETE` when the offline renderer made no request for a resource absent from the uploaded package. It is `INCOMPLETE` when at least one such request was blocked. Neither value establishes completeness of the original website, authenticity of `originalLink`, or visual equivalence across browsers. The public `createdAt` is the job acceptance time, not a trusted timestamp from the file.

## Current boundaries

- The frontend has no dedicated submission page yet; clients can use the multipart API directly.
- JavaScript is intentionally disabled for imported snapshots. Dynamic behavior after the original save cannot be reconstructed from the package alone.
- The current worker Dockerfile installs Chromium with `--no-shell`, while the offline renderer's headless launch uses Playwright's default headless shell. Containerized file imports therefore require aligning browser installation or running the worker with `Archive:Browser:Headless=false` under Xvfb. The local development import was verified with the headless shell installed.
- Pixel-level comparison against a Safari reference has not been established. Missing assets, font differences, and frame mapping can change the replay.

To add another upload format, extend the `SourceType` PostgreSQL mapping and migration, register a matching API probe and worker decoder, and update the public API types and format labels. Keep parsing separate from the common offline replay path and preserve the same input limits and network isolation.
