<script lang="ts">
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import { page } from "$app/state";
	import type { SnapshotResponse } from "$api";
	import CopySourceButton from "$lib/components/archive/CopySourceButton.svelte";
	import SnapshotMetadata from "$lib/components/archive/SnapshotMetadata.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	let { snapshot, layout = "sheet" }: { snapshot: SnapshotResponse; layout?: "sheet" | "desktop" } = $props();
	let failedImage = $state<string | null>(null);
	const source = $derived(snapshot.sourceUrl);
	const archivedHref = $derived(
		`/snapshots/${snapshot.id}?from=${encodeURIComponent(page.url.pathname + page.url.search + "#snapshot-register")}`
	);
</script>

<div class="preview-content" data-component="snapshot-preview-content" data-layout={layout}>
	<div class="archive-frame">
		{#if !snapshot.screenshotUrl || failedImage === snapshot.screenshotUrl}
			<div class="image-error">
				<CircleAlert aria-hidden="true" />
				<h4>Preview image unavailable</h4>
				<p>The image could not be loaded. The record and archived page remain available.</p>
			</div>
		{:else}
			<img
				src={snapshot.screenshotUrl}
				alt={`Archived screenshot of ${snapshot.title}`}
				onerror={(event) => {
					const image = event.currentTarget;
					if (image instanceof HTMLImageElement) failedImage = image.currentSrc || image.src;
				}}
			/>
		{/if}
	</div>
	<div class="preview-details">
		<h3>{snapshot.title}</h3>
		{#if source}
			<div class="preview-source-row">
				<a class="preview-source" href={source} target="_blank" rel="noreferrer">{source}</a>
				<CopySourceButton value={source} />
			</div>
		{/if}
		<div class="preview-metadata">
			<SnapshotMetadata {snapshot} />
		</div>
		<div class="preview-actions">
			<Button href={archivedHref} variant="primary">Open archived page <span aria-hidden="true">→</span></Button>
		</div>
	</div>
</div>

<style>
	.archive-frame {
		background: var(--archive-frame);
		padding: 0.5rem;
		max-height: min(42vh, 28rem);
		overflow-y: auto;
	}
	.archive-frame img {
		display: block;
		width: 100%;
		height: auto;
	}
	.preview-details {
		padding: 1.5rem 1rem;
		min-width: 0;
	}
	h3 {
		overflow-wrap: anywhere;
		font-family: var(--font-editorial);
		font-size: 1.5rem;
		font-weight: 500;
		line-height: 1.25;
		margin: 0 0 0.75rem;
	}
	.preview-source-row {
		display: flex;
		min-width: 0;
		align-items: start;
		gap: 0.25rem;
	}
	.preview-source {
		display: block;
		min-width: 0;
		flex: 1;
		font-family: var(--font-record);
		font-size: 0.8125rem;
		overflow-wrap: anywhere;
		margin: 0;
	}
	.preview-actions {
		display: grid;
		gap: 0.5rem;
	}
	.preview-metadata {
		margin-block: 1.5rem;
	}
	.preview-actions :global(a),
	.preview-actions :global(button) {
		width: 100%;
	}
	.image-error p {
		color: var(--marginal-note);
		font-size: 0.9375rem;
	}
	.image-error {
		background: var(--reading-room);
		padding: 1rem;
	}
	.image-error :global(svg) {
		width: 1.5rem;
		color: var(--time-marker);
	}
	.image-error h4 {
		font-size: 1rem;
		font-weight: 600;
	}
	.preview-content[data-layout="desktop"] {
		display: grid;
		min-height: 0;
		flex: 1;
		grid-template-columns: minmax(0, 1.45fr) minmax(20rem, 0.55fr);
		grid-template-rows: minmax(0, 1fr);
	}
	.preview-content[data-layout="desktop"] .archive-frame {
		display: flex;
		grid-column: 1;
		grid-row: 1;
		min-height: 0;
		max-height: none;
		align-items: center;
		justify-content: center;
		overflow: hidden;
		padding: 1rem;
	}
	.preview-content[data-layout="desktop"] .archive-frame img {
		width: 100%;
		height: 100%;
		object-fit: contain;
	}
	.preview-content[data-layout="desktop"] .preview-details {
		grid-column: 2;
		grid-row: 1;
		min-height: 0;
		overflow-y: auto;
		border-left: 1px solid var(--border-trace);
		padding: 2rem;
	}
	.preview-content[data-layout="desktop"] h3 {
		font-size: clamp(1.75rem, 2.4vw, 2.5rem);
	}
	.preview-content[data-layout="desktop"] .preview-source-row {
		align-items: center;
	}
	.preview-content[data-layout="desktop"] .preview-source {
		overflow: hidden;
		line-height: 2.75rem;
		overflow-wrap: normal;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
</style>
