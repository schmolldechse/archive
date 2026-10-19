<script lang="ts">
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import Image from "@lucide/svelte/icons/image";
	import type { SnapshotResponse } from "$api";
	import ArchiveCaptureTime from "./ArchiveCaptureTime.svelte";
	import Badge from "$lib/components/ui/Badge.svelte";
	import type { HTMLButtonAttributes } from "svelte/elements";
	import CopySourceButton from "./CopySourceButton.svelte";
	import ArchiveAttribution from "./ArchiveAttribution.svelte";
	import SnapshotTags from "./SnapshotTags.svelte";
	import { formatBytes } from "./format";

	interface ArchiveRecordProps {
		snapshot: SnapshotResponse;
		selected?: boolean;
		previewExpanded?: boolean;
		onInspect: (snapshot: SnapshotResponse, trigger: HTMLButtonElement) => void;
	}

	let { snapshot, selected = false, previewExpanded, onInspect }: ArchiveRecordProps = $props();

	const source = $derived(snapshot.sourceUrl);
	const qualityLabel = $derived(snapshot.quality === "COMPLETE" ? "Complete" : "Incomplete");
	let failedScreenshot = $state<string | null>(null);
	const screenshot = $derived(snapshot.screenshotUrl !== failedScreenshot ? snapshot.screenshotUrl : null);

	const handleInspect: NonNullable<HTMLButtonAttributes["onclick"]> = (event) => {
		onInspect(snapshot, event.currentTarget);
	};
</script>

<article class="archive-record" data-component="archive-record" data-state={selected ? "selected" : "idle"}>
	<div class="archive-record__provenance">
		<ArchiveCaptureTime value={snapshot.createdAt} layout="compact" />
		<ArchiveAttribution creator={snapshot.createdBy} layout="compact" />
	</div>

	<div class="archive-record__main">
		<div class="archive-record__title">
			<h3>{snapshot.title}</h3>
			<Badge variant={snapshot.quality === "COMPLETE" ? "success" : "warning"} size="compact">
				{qualityLabel}
			</Badge>
		</div>

		{#if source}
			<div class="archive-record__source">
				<a class="archive-record__url" href={source} target="_blank" rel="noreferrer" title={source}>{source}</a>
				<CopySourceButton value={source} label={`Copy source URL for ${snapshot.title}`} />
			</div>
		{/if}

		{#if snapshot.description}
			<p class="archive-record__description">{snapshot.description}</p>
		{/if}
		<SnapshotTags tags={snapshot.tags} />
	</div>

	<button
		type="button"
		class="archive-record__preview"
		aria-label={`Inspect ${snapshot.title}`}
		aria-haspopup="dialog"
		aria-controls={previewExpanded ? "snapshot-preview" : undefined}
		aria-expanded={previewExpanded ?? selected}
		onclick={handleInspect}
	>
		{#if screenshot}
			<img
				src={screenshot}
				alt=""
				loading="lazy"
				decoding="async"
				onerror={() => {
					failedScreenshot = screenshot;
				}}
			/>
		{:else}
			<span class="archive-record__preview-empty"><Image aria-hidden="true" />Preview unavailable</span>
		{/if}
		<span class="archive-record__preview-action">Inspect capture <ArrowRight aria-hidden="true" /></span>
	</button>

	<footer class="archive-record__footer">
		<dl class="archive-record__meta">
			<div>
				<dt>Resources</dt>
				<dd>{String(snapshot.resourceCount).padStart(3, "0")}</dd>
			</div>
			<div>
				<dt>Size</dt>
				<dd>{formatBytes(Number(snapshot.storageBytes))}</dd>
			</div>
		</dl>
	</footer>
</article>

<style>
	.archive-record {
		position: relative;
		display: grid;
		grid-template-columns: minmax(0, 1fr) 13rem;
		grid-template-areas:
			"provenance preview"
			"main preview"
			"footer footer";
		column-gap: 2.5rem;
		border-block: 1px solid var(--border-trace);
		padding: 1rem 1rem 1.5rem;
		transition: background-color 150ms ease;
	}

	.archive-record:hover,
	.archive-record[data-state="selected"] {
		background: color-mix(in srgb, var(--archive-layer) 55%, transparent);
	}
	.archive-record__provenance {
		grid-area: provenance;
		display: grid;
		grid-template-columns: minmax(10rem, auto) minmax(12rem, 1fr);
		gap: 2rem;
		min-width: 0;
	}

	.archive-record__main {
		grid-area: main;
		min-width: 0;
		padding-top: 1.5rem;
	}

	.archive-record__main h3 {
		overflow-wrap: anywhere;
		margin: 0;
		font-family: var(--font-editorial);
		font-size: clamp(1.35rem, 2.5vw, 1.75rem);
		font-weight: 500;
		line-height: 1.18;
	}

	.archive-record__title {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 1rem;
	}

	.archive-record__title :global(.badge) {
		flex: none;
		margin-top: 0.1875rem;
	}

	.archive-record__source {
		display: flex;
		min-width: 0;
		align-items: center;
		gap: 0.25rem;
		margin-top: 0.5rem;
	}

	.archive-record__url {
		display: -webkit-box;
		min-width: 0;
		max-width: 100%;
		-webkit-box-orient: vertical;
		-webkit-line-clamp: 2;
		line-clamp: 2;
		overflow: hidden;
		color: var(--register-mark);
		font-family: var(--font-record);
		font-size: 0.8125rem;
		line-height: 1.54;
		overflow-wrap: anywhere;
	}

	.archive-record__description {
		max-width: 45rem;
		margin: 0.875rem 0 0;
		color: var(--marginal-note);
		font-size: 0.9375rem;
		line-height: 1.55;
	}

	.archive-record__preview {
		grid-area: preview;
		position: relative;
		align-self: start;
		min-width: 0;
		margin-top: 1rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		padding: 0;
		aspect-ratio: 16 / 10;
		overflow: hidden;
		background: var(--archive-layer);
		color: var(--reading-ink);
		cursor: pointer;
	}
	.archive-record__preview:hover {
		border-color: var(--reading-ink);
	}
	.archive-record__preview img {
		display: block;
		width: 100%;
		height: 100%;
		object-fit: cover;
		object-position: top;
	}
	.archive-record__preview-empty {
		display: grid;
		place-items: center;
		align-content: center;
		gap: 0.5rem;
		height: 100%;
		color: var(--marginal-note);
		font-size: 0.75rem;
	}
	.archive-record__preview-empty :global(svg) {
		width: 1.5rem;
		height: 1.5rem;
		stroke-width: 1.5;
	}
	.archive-record__preview-action {
		position: absolute;
		inset-inline: 0;
		bottom: 0;
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 0.5rem;
		padding: 0.5rem 0.625rem;
		border-top: 1px solid var(--border-trace);
		background: var(--reading-room);
		font-family: var(--font-interface);
		font-size: 0.75rem;
		font-weight: 600;
	}
	.archive-record__preview-action :global(svg) {
		width: 1rem;
		height: 1rem;
	}
	.archive-record__footer {
		grid-area: footer;
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 1.5rem;
		margin-top: 1.5rem;
		border-top: 1px solid color-mix(in srgb, var(--border-trace) 55%, transparent);
		padding-top: 1rem;
	}

	.archive-record__meta {
		display: flex;
		flex-wrap: wrap;
		gap: 1rem 2rem;
		margin: 0;
	}

	.archive-record__meta > div {
		display: flex;
		align-items: baseline;
		gap: 0.5rem;
	}

	.archive-record__meta dt,
	.archive-record__meta dd {
		margin: 0;
		font-size: 0.75rem;
	}

	.archive-record__meta dt {
		color: var(--marginal-note);
	}

	.archive-record__meta dd {
		font-family: var(--font-record);
		font-variant-numeric: tabular-nums;
		text-align: left;
	}

	@media (max-width: 56rem) {
		.archive-record {
			grid-template-columns: minmax(0, 1fr) 11rem;
			column-gap: 1.5rem;
		}
	}

	@media (max-width: 48rem) {
		.archive-record {
			grid-template-columns: 1fr;
			grid-template-areas:
				"preview"
				"provenance"
				"main"
				"footer";
			padding: 0 0 1.5rem;
		}
		.archive-record__provenance {
			grid-template-columns: repeat(2, minmax(0, 1fr));
			gap: 1rem;
			padding-inline: 1rem;
		}
		.archive-record__main,
		.archive-record__footer {
			margin-inline: 1rem;
		}
		.archive-record__preview {
			margin: 0;
			border-inline: 0;
			border-radius: 0;
			aspect-ratio: 2 / 1;
		}
	}

	@media (max-width: 34rem) {
		.archive-record__provenance {
			grid-template-columns: 1fr;
		}
		.archive-record__footer {
			align-items: stretch;
			flex-direction: column;
		}
		.archive-record__meta {
			justify-content: space-between;
		}
		.archive-record__title {
			align-items: flex-start;
			flex-direction: column;
			gap: 0.75rem;
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.archive-record {
			transition: none;
		}
	}
</style>
