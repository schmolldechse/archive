<script lang="ts">
	import type { SnapshotResponse } from "$api";
	import LocalTimestamp from "$lib/components/site/LocalTimestamp.svelte";
	import Badge from "$lib/components/ui/Badge.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import type { HTMLButtonAttributes } from "svelte/elements";
	import CopySourceButton from "./CopySourceButton.svelte";
	import { formatBytes } from "./format";

	interface ArchiveRecordProps {
		snapshot: SnapshotResponse;
		selected?: boolean;
		previewExpanded?: boolean;
		onInspect: (snapshot: SnapshotResponse, trigger: HTMLButtonElement) => void;
	}

	let { snapshot, selected = false, previewExpanded, onInspect }: ArchiveRecordProps = $props();

	const source = $derived(snapshot.sourceUrl ?? snapshot.originalLink);
	const qualityLabel = $derived(snapshot.quality === "COMPLETE" ? "Complete" : "Incomplete");

	const handleInspect: NonNullable<HTMLButtonAttributes["onclick"]> = (event) => {
		onInspect(snapshot, event.currentTarget);
	};
</script>

<article class="archive-record" data-component="archive-record" data-state={selected ? "selected" : "idle"}>
	<div class="archive-record__time">
		<LocalTimestamp value={snapshot.createdAt} format="date" class="archive-record__day" />
		<LocalTimestamp value={snapshot.createdAt} format="time" class="archive-record__zone" />
	</div>

	<div class="archive-record__main">
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
		{#if snapshot.tags.length}
			<ul class="archive-record__tags" aria-label="Snapshot tags">
				{#each snapshot.tags as tag (tag)}<li><span>#</span>{tag}</li>{/each}
			</ul>
		{/if}

	</div>

	<div class="archive-record__side">
		<dl class="archive-record__meta">
			<div>
				<dt>Quality</dt>
				<dd>
					<Badge variant={snapshot.quality === "COMPLETE" ? "success" : "warning"} size="compact">
						{qualityLabel}
					</Badge>
				</dd>
			</div>
			<div>
				<dt>Resources</dt>
				<dd>{String(snapshot.resourceCount).padStart(3, "0")}</dd>
			</div>
			<div>
				<dt>Size</dt>
				<dd>{formatBytes(Number(snapshot.storageBytes))}</dd>
			</div>
		</dl>
		<div class="archive-record__actions">
			<Button
				variant="secondary"
				class="archive-record__action"
				aria-haspopup="dialog"
				aria-controls={previewExpanded ? "snapshot-preview" : undefined}
				aria-expanded={previewExpanded ?? selected}
				onclick={handleInspect}
			>
				Inspect snapshot <span aria-hidden="true">→</span>
			</Button>
		</div>
	</div>
</article>

<style>
	.archive-record {
		position: relative;
		display: grid;
		grid-template-columns: 8.5rem minmax(0, 1fr) 10.5rem;
		gap: 2rem;
		border-bottom: 1px solid var(--border-trace);
		padding: 2rem 1rem;
		transition: background-color 150ms ease;
	}

	.archive-record:hover,
	.archive-record[data-state="selected"] {
		background: color-mix(in srgb, var(--archive-layer) 55%, transparent);
	}

	.archive-record__time {
		padding-top: 0.1875rem;
	}

	.archive-record__time :global(time) {
		display: block;
	}

	:global(.archive-record__day) {
		color: var(--reading-ink);
		font-size: 0.8125rem;
		font-weight: 500;
	}

	:global(.archive-record__zone) {
		margin-top: 0.375rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		line-height: 1.45;
	}

	.archive-record__main {
		min-width: 0;
	}

	.archive-record__main h3 {
		margin: 0;
		font-family: var(--font-editorial);
		font-size: clamp(1.35rem, 2.5vw, 1.75rem);
		font-weight: 500;
		line-height: 1.18;
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

	.archive-record__side {
		display: flex;
		min-width: 0;
		flex-direction: column;
		justify-content: space-between;
		gap: 1.5rem;
	}
	.archive-record__actions {
		display: flex;
	}
	.archive-record__tags {
		list-style: none;
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem 0.75rem;
		padding: 0;
		margin: 0.75rem 0 0;
		font-size: 0.75rem;
		color: var(--marginal-note);
	}
	.archive-record__tags li {
		overflow-wrap: anywhere;
	}
	.archive-record__tags li span {
		color: var(--register-mark);
	}

	.archive-record__actions :global(.archive-record__action) {
		width: 100%;
		white-space: nowrap;
	}

	.archive-record__meta {
		display: grid;
		align-content: start;
		gap: 0.75rem;
		margin: 0;
	}

	.archive-record__meta > div {
		display: grid;
		grid-template-columns: 1fr auto;
		align-items: center;
		gap: 0.75rem;
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
		text-align: right;
	}

	@media (max-width: 64rem) {
		.archive-record {
			grid-template-columns: 7rem minmax(0, 1fr);
		}

		.archive-record__side {
			grid-column: 2;
			flex-direction: row;
			align-items: end;
			flex-wrap: wrap;
			gap: 1rem;
		}

		.archive-record__meta {
			display: grid;
			flex: 1 1 24rem;
			grid-template-columns: repeat(3, minmax(0, 1fr));
			gap: 0.75rem;
		}

		.archive-record__actions {
			flex: 0 0 auto;
		}

		.archive-record__meta > div {
			justify-content: start;
		}

		.archive-record__meta dd {
			text-align: left;
		}
	}

	@media (max-width: 48rem) {
		.archive-record {
			grid-template-columns: 1fr;
			gap: 1.125rem;
			padding: 1.75rem 1rem;
		}

		.archive-record__time {
			display: flex;
			align-items: baseline;
			gap: 0.625rem;
		}

		:global(.archive-record__zone) {
			margin-top: 0;
		}

		.archive-record__side {
			grid-column: auto;
			flex-direction: column;
			align-items: stretch;
		}

		.archive-record__meta {
			flex: none;
		}

		.archive-record__actions {
			flex: none;
		}
	}

	@media (max-width: 34rem) {
		.archive-record__meta {
			grid-template-columns: 1fr;
			gap: 0;
		}

		.archive-record__meta > div {
			display: flex;
			justify-content: space-between;
			border-top: 1px solid color-mix(in srgb, var(--border-trace) 55%, transparent);
			padding-block: 0.5rem;
		}

		.archive-record__meta dd {
			text-align: right;
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.archive-record {
			transition: none;
		}
	}
</style>
