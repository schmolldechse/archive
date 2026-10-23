<script lang="ts">
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import Check from "@lucide/svelte/icons/check";
	import Image from "@lucide/svelte/icons/image";
	import type { SnapshotResponse } from "$api";
	import ArchiveCaptureTime from "./ArchiveCaptureTime.svelte";
	import type { HTMLButtonAttributes } from "svelte/elements";
	import CopySourceButton from "./CopySourceButton.svelte";
	import ArchiveAttribution from "./ArchiveAttribution.svelte";
	import SnapshotTagList from "./SnapshotTagList.svelte";
	import { formatBytes } from "./format";

	interface ArchiveRecordProps {
		snapshot: SnapshotResponse;
		selected?: boolean;
		previewExpanded?: boolean;
		onInspect: (snapshot: SnapshotResponse, trigger: HTMLButtonElement) => void;
	}

	let { snapshot, selected = false, previewExpanded, onInspect }: ArchiveRecordProps = $props();

	const source = $derived(snapshot.sourceUrl);
	const previewIsOpen = $derived(previewExpanded ?? selected);
	let failedScreenshot = $state<string | null>(null);
	const screenshot = $derived(snapshot.screenshotUrl !== failedScreenshot ? snapshot.screenshotUrl : null);

	const handleInspect: NonNullable<HTMLButtonAttributes["onclick"]> = (event) => {
		onInspect(snapshot, event.currentTarget);
	};
</script>

<article class="archive-record" data-component="archive-record" data-state={selected ? "selected" : "idle"}>
	<div class="archive-record__main">
		<div class="archive-record__register">
			<ArchiveCaptureTime value={snapshot.createdAt} layout="compact" />
		</div>

		<h3>{snapshot.title}</h3>

		{#if source}
			<div class="archive-record__source">
				<a class="archive-record__url" href={source} target="_blank" rel="noreferrer" title={source}>{source}</a>
				<CopySourceButton value={source} label={`Copy source URL for ${snapshot.title}`} />
			</div>
		{/if}

		{#if snapshot.description}
			<p class="archive-record__description">{snapshot.description}</p>
		{/if}

		<SnapshotTagList tags={snapshot.tags} />
	</div>

	<button
		type="button"
		class="archive-record__preview"
		data-state={previewIsOpen ? "selected" : "idle"}
		aria-label={`Preview archived page: ${snapshot.title}`}
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
		<span class="archive-record__preview-action">
			<span>{previewIsOpen ? "Preview open" : "Preview capture"}</span>
			{#if previewIsOpen}<Check aria-hidden="true" />{:else}<ArrowRight aria-hidden="true" />{/if}
		</span>
	</button>

	<footer class="archive-record__footer">
		<div class="archive-record__attribution">
			{#if snapshot.createdBy}
				<ArchiveAttribution creator={snapshot.createdBy} layout="compact" label={null} />
			{:else}
				<p>Submitted anonymously</p>
			{/if}
		</div>
		<dl class="archive-record__meta">
			<div>
				<dt>Resources</dt>
				<dd>{snapshot.resourceCount === null ? "—" : String(snapshot.resourceCount).padStart(3, "0")}</dd>
			</div>
			<div>
				<dt>Stored</dt>
				<dd>{snapshot.storageBytes === null ? "—" : formatBytes(Number(snapshot.storageBytes))}</dd>
			</div>
		</dl>
	</footer>
</article>

<style>
	.archive-record {
		position: relative;
		display: grid;
		grid-template-columns: minmax(0, 1fr) minmax(14rem, 0.38fr);
		grid-template-areas:
			"main preview"
			"footer footer";
		column-gap: clamp(1.5rem, 4vw, 3.5rem);
		border-block: 1px solid var(--border-trace);
		padding: 1.5rem 1rem 1.125rem;
		transition: background-color 150ms ease;
	}

	.archive-record:hover {
		background: color-mix(in srgb, var(--archive-layer) 35%, transparent);
	}

	.archive-record[data-state="selected"] {
		background: color-mix(in srgb, var(--register-mark) 5%, transparent);
	}

	.archive-record__main {
		grid-area: main;
		min-width: 0;
	}

	.archive-record__register {
		display: flex;
		min-width: 0;
		align-items: flex-start;
		justify-content: space-between;
		gap: 1rem;
		margin-bottom: 1rem;
	}

	.archive-record__main h3 {
		max-width: 34ch;
		overflow-wrap: anywhere;
		margin: 0;
		font-family: var(--font-editorial);
		font-size: clamp(1.5rem, 2.8vw, 2.125rem);
		font-weight: 500;
		letter-spacing: -0.015em;
		line-height: 1.12;
	}

	.archive-record__source {
		display: flex;
		min-width: 0;
		align-items: center;
		gap: 0.375rem;
		margin-top: 0.75rem;
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
		max-width: 52rem;
		margin: 1rem 0 0;
		color: var(--marginal-note);
		font-size: 0.9375rem;
		line-height: 1.55;
	}

	.archive-record__preview {
		grid-area: preview;
		position: relative;
		display: flex;
		min-width: 0;
		flex-direction: column;
		align-self: start;
		margin: 0;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		padding: 0;
		overflow: hidden;
		background: var(--archive-layer);
		color: var(--reading-ink);
		text-align: left;
		cursor: pointer;
		transition:
			border-color 150ms ease,
			background-color 150ms ease;
	}

	.archive-record__preview:hover,
	.archive-record__preview[data-state="selected"] {
		border-color: var(--register-mark);
	}

	.archive-record__preview:focus-visible {
		outline: 3px solid var(--register-mark);
		outline-offset: 3px;
	}

	.archive-record__preview img {
		display: block;
		width: 100%;
		aspect-ratio: 16 / 10;
		object-fit: cover;
		object-position: top;
	}

	.archive-record__preview-empty {
		display: grid;
		min-height: 9rem;
		place-items: center;
		align-content: center;
		gap: 0.5rem;
		color: var(--marginal-note);
		font-size: 0.8125rem;
	}

	.archive-record__preview-empty :global(svg) {
		width: 1.5rem;
		height: 1.5rem;
		stroke-width: 1.5;
	}

	.archive-record__preview-action {
		display: flex;
		min-height: 2.75rem;
		align-items: center;
		justify-content: space-between;
		gap: 0.75rem;
		border-top: 1px solid var(--border-trace);
		padding: 0.5rem 0.75rem;
		font-size: 0.875rem;
		font-weight: 600;
	}

	.archive-record__preview-action :global(svg) {
		width: 1rem;
		height: 1rem;
		flex: none;
	}

	.archive-record__footer {
		grid-area: footer;
		display: flex;
		min-width: 0;
		align-items: center;
		justify-content: space-between;
		flex-wrap: wrap;
		gap: 0.75rem 1.5rem;
		margin-top: 1.5rem;
		border-top: 1px solid color-mix(in srgb, var(--border-trace) 65%, transparent);
		padding-top: 0.875rem;
	}

	.archive-record__attribution {
		min-width: 0;
		color: var(--marginal-note);
		font-size: 0.8125rem;
		line-height: 1.4;
	}

	.archive-record__attribution p {
		margin: 0;
	}

	.archive-record__attribution :global(.archive-attribution__identity) {
		min-height: 2rem;
	}

	.archive-record__meta {
		display: flex;
		flex-wrap: wrap;
		gap: 0.75rem 1.5rem;
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

	@media (max-width: 48rem) {
		.archive-record {
			grid-template-columns: minmax(0, 1fr);
			grid-template-areas:
				"main"
				"preview"
				"footer";
			row-gap: 1.25rem;
			padding: 1.25rem 0 1rem;
		}

		.archive-record__main,
		.archive-record__footer {
			margin-inline: 1rem;
		}

		.archive-record__preview {
			margin-inline: 1rem;
		}

		.archive-record__preview img {
			aspect-ratio: 16 / 9;
			object-fit: cover;
		}

		.archive-record__footer {
			margin-top: 0;
		}
	}

	@media (max-width: 34rem) {
		.archive-record__main h3 {
			font-size: clamp(1.5rem, 7vw, 1.875rem);
		}

		.archive-record__footer {
			align-items: flex-start;
			flex-direction: column;
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.archive-record,
		.archive-record__preview {
			transition: none;
		}
	}
</style>
