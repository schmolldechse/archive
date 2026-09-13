<script lang="ts">
	import type { SnapshotResponse } from "$api";
	import LocalTimestamp from "$lib/components/site/LocalTimestamp.svelte";
	import Badge from "$lib/components/ui/Badge.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import type { HTMLButtonAttributes } from "svelte/elements";

	interface ArchiveRecordProps {
		snapshot: SnapshotResponse;
		selected?: boolean;
		onInspect: (snapshot: SnapshotResponse, trigger: HTMLButtonElement) => void;
		onCopy: (snapshot: SnapshotResponse) => void;
	}

	let { snapshot, selected = false, onInspect, onCopy }: ArchiveRecordProps = $props();

	const source = $derived(snapshot.sourceUrl ?? snapshot.originalLink ?? "Uploaded HTML document");
	const qualityLabel = $derived(snapshot.quality === "COMPLETE" ? "Complete" : "Incomplete");

	function formatBytes(bytes: number): string {
		if (bytes < 1024) return `${bytes} B`;

		const units = ["KB", "MB", "GB", "TB"];
		const unitIndex = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)) - 1, units.length - 1);
		const value = bytes / 1024 ** (unitIndex + 1);
		return `${value.toFixed(value >= 10 ? 0 : 1)} ${units[unitIndex]}`;
	}

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
		<div class="archive-record__title-line">
			<h3>{snapshot.title}</h3>
			{#if selected}<span class="archive-record__selection">Inspecting</span>{/if}
		</div>

		{#if snapshot.sourceUrl || snapshot.originalLink}
			<a class="archive-record__url" href={source} target="_blank" rel="noreferrer">{source}</a>
		{:else}
			<span class="archive-record__url">{source}</span>
		{/if}

		{#if snapshot.description}
			<p class="archive-record__description">{snapshot.description}</p>
		{/if}

		<div class="archive-record__actions">
			<Button
				variant="text"
				class="archive-record__action"
				aria-haspopup="dialog"
				aria-controls="snapshot-preview"
				aria-expanded={selected}
				onclick={handleInspect}
			>
				Inspect snapshot&nbsp;→
			</Button>
			{#if snapshot.sourceUrl || snapshot.originalLink}
				<Button variant="text" class="archive-record__action" onclick={() => onCopy(snapshot)}>Copy source URL</Button>
			{/if}
		</div>
	</div>

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
</article>

<style>
	.archive-record {
		position: relative;
		display: grid;
		grid-template-columns: 8.5rem minmax(0, 1fr) 10.5rem;
		gap: 2rem;
		border-bottom: 1px solid var(--border-trace);
		padding: 2rem 0;
		transition: background-color 150ms ease;
	}

	.archive-record:hover,
	.archive-record[data-state="selected"] {
		background: color-mix(in srgb, var(--archive-layer) 55%, transparent);
	}

	.archive-record[data-state="selected"] {
		padding-inline: 1rem;
	}

	.archive-record[data-state="selected"]::before {
		position: absolute;
		inset: 0 auto 0 0;
		width: 3px;
		background: var(--time-marker);
		content: "";
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
		font-size: 0.6875rem;
		line-height: 1.45;
	}

	.archive-record__main {
		min-width: 0;
	}

	.archive-record__title-line {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 1rem;
	}

	.archive-record__main h3 {
		margin: 0;
		font-family: var(--font-editorial);
		font-size: clamp(1.35rem, 2.5vw, 1.75rem);
		font-weight: 500;
		line-height: 1.18;
	}

	.archive-record__selection {
		flex: none;
		color: var(--time-marker);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.06em;
		text-transform: uppercase;
	}

	.archive-record__url {
		display: inline-block;
		max-width: 100%;
		margin-top: 0.5rem;
		color: var(--register-mark);
		font-family: var(--font-record);
		font-size: 0.8125rem;
		line-height: 1.54;
		overflow-wrap: anywhere;
	}

	span.archive-record__url {
		color: var(--marginal-note);
	}

	.archive-record__description {
		max-width: 45rem;
		margin: 0.875rem 0 0;
		color: var(--marginal-note);
		font-size: 0.9375rem;
		line-height: 1.55;
	}

	.archive-record__actions {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem 1.25rem;
		margin-top: 0.75rem;
	}

	.archive-record__actions :global(.archive-record__action) {
		min-width: 0;
		padding-inline: 0;
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

		.archive-record__meta {
			display: grid;
			grid-column: 2;
			grid-template-columns: repeat(3, minmax(0, 1fr));
			gap: 0.75rem;
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
			padding: 1.75rem 0;
		}

		.archive-record__time {
			display: flex;
			align-items: baseline;
			gap: 0.625rem;
		}

		:global(.archive-record__zone) {
			margin-top: 0;
		}

		.archive-record__meta {
			grid-column: auto;
		}
	}

	@media (max-width: 34rem) {
		.archive-record__title-line {
			display: block;
		}

		.archive-record__selection {
			display: inline-block;
			margin-top: 0.5rem;
		}

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
