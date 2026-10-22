<script lang="ts">
	import { page } from "$app/state";
	import type { SnapshotHistoryResponse } from "$api";
	import { pageHref } from "$lib/admin/navigation";
	import { historyPresentation } from "$lib/components/history/history-events";
	import SnapshotHistoryEvent from "$lib/components/history/SnapshotHistoryEvent.svelte";
	import ArchiveTimestamp from "$lib/components/site/ArchiveTimestamp.svelte";
	import AdminPagination from "$lib/components/admin/AdminPagination.svelte";

	let {
		history,
		title = "Snapshot history",
		compact = false,
		paginated = true
	}: {
		history: SnapshotHistoryResponse;
		title?: string;
		compact?: boolean;
		paginated?: boolean;
	} = $props();

	const total = $derived(Number(history.total));
</script>

<section class="snapshot-history" class:compact aria-labelledby="snapshot-history-title">
	<header class="history-header">
		<div>
			<p class="history-kicker">Record log</p>
			<h2 id="snapshot-history-title">{title}</h2>
		</div>
		<p class="history-count" aria-label={`${total} history events`}>
			<strong>{total}</strong><span>{total === 1 ? "event" : "events"}</span>
		</p>
	</header>

	{#if !history.items.length}
		<p class="history-empty">No history events are recorded for this snapshot.</p>
	{:else}
		<ol class="history-list">
			{#each history.items as item (item.sequence)}
				{@const presentation = historyPresentation(item)}
				<li class="history-item" data-tone={presentation.tone}>
					<div class="history-time"><ArchiveTimestamp value={item.createdAt} density="compact" /></div>
					<div class="history-rail" aria-hidden="true"><span class="history-mark"></span></div>
					<SnapshotHistoryEvent {item} />
				</li>
			{/each}
		</ol>
		{#if !paginated && total > history.items.length}
			<p class="history-truncated">Showing the {history.items.length} newest events.</p>
		{:else if paginated && total > Number(history.pageSize)}
			<div class="history-pagination">
				<AdminPagination
					{total}
					pageSize={Number(history.pageSize)}
					currentPage={Number(history.page)}
					href={(value) => pageHref(page.url, "historyPage", value)}
					label="Snapshot history pages"
				/>
			</div>
		{/if}
	{/if}
</section>

<style>
	.history-header {
		display: flex;
		align-items: end;
		justify-content: space-between;
		gap: 1.5rem;
		border-bottom: 1px solid var(--border-trace);
		padding-bottom: 1rem;
		margin-bottom: 1.75rem;
	}
	.history-kicker {
		margin: 0 0 0.375rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	h2 {
		margin: 0;
		font: 500 2rem/1.18 var(--font-editorial);
		letter-spacing: -0.015em;
	}
	.history-count {
		display: flex;
		align-items: baseline;
		gap: 0.375rem;
		margin: 0 0 0.25rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
		white-space: nowrap;
	}
	.history-count strong {
		color: var(--reading-ink);
		font: 500 0.875rem/1.4 var(--font-record);
		font-variant-numeric: tabular-nums;
	}
	.history-list {
		list-style: none;
		margin: 0;
		padding: 0;
	}
	.history-item {
		--history-tone: var(--marginal-note);
		display: grid;
		grid-template-columns: minmax(10rem, 0.8fr) 1.5rem minmax(0, 2fr);
		gap: 0 1.25rem;
		position: relative;
		padding-bottom: 2rem;
	}
	.history-item:last-child {
		padding-bottom: 0.5rem;
	}
	.history-item[data-tone="register"] {
		--history-tone: var(--register-mark);
	}
	.history-item[data-tone="success"] {
		--history-tone: var(--preservation-green);
	}
	.history-item[data-tone="warning"] {
		--history-tone: var(--warning-ochre);
	}
	.history-item[data-tone="critical"] {
		--history-tone: var(--time-marker);
	}
	.history-time {
		display: flex;
		align-items: center;
		grid-column: 1;
		align-self: start;
		justify-self: end;
		padding-top: 0.25rem;
	}
	.history-rail {
		grid-column: 2;
		grid-row: 1;
		position: relative;
		display: flex;
		justify-content: center;
	}
	.history-item:not(:last-child) .history-rail::after {
		position: absolute;
		inset-block: 0.75rem -2rem;
		border-inline-start: 1px solid var(--border-trace);
		content: "";
	}
	.history-mark {
		position: relative;
		z-index: 1;
		width: 0.75rem;
		height: 0.75rem;
		margin-top: 0.375rem;
		border: 2px solid var(--history-tone);
		border-radius: 50%;
		background: var(--reading-room);
	}
	.history-item[data-tone="success"] .history-mark,
	.history-item[data-tone="warning"] .history-mark,
	.history-item[data-tone="critical"] .history-mark {
		background: var(--history-tone);
	}
	.history-item :global(.history-event) {
		grid-column: 3;
	}
	.history-item:last-child :global(.history-event) {
		border-bottom: 0;
		padding-bottom: 0;
	}
	.history-empty,
	.history-truncated {
		margin: 0;
		border-block: 1px solid var(--border-trace);
		padding: 1.5rem 0;
		color: var(--marginal-note);
	}
	.history-truncated {
		border-block: 0;
		border-top: 1px solid var(--border-trace);
		font-size: 0.875rem;
	}
	.history-pagination {
		border-top: 1px solid var(--border-trace);
		margin-top: 1.25rem;
		padding-top: 1.25rem;
	}
	.compact .history-header {
		margin-bottom: 1.25rem;
	}
	.compact h2 {
		font-size: 1.5rem;
	}
	.compact .history-item {
		grid-template-columns: 1.25rem minmax(0, 1fr);
		grid-template-rows: auto auto;
		gap: 0.375rem 0.75rem;
		padding-bottom: 1.25rem;
	}
	.compact .history-time {
		grid-column: 2;
		grid-row: 1;
		justify-self: start;
		padding-top: 0;
	}
	.compact .history-rail {
		grid-column: 1;
		grid-row: 1 / span 2;
		justify-content: flex-start;
		padding-inline-start: 0.25rem;
	}
	.compact .history-item :global(.history-event) {
		grid-column: 2;
		grid-row: 2;
	}
	@media (max-width: 48rem) {
		.history-header {
			align-items: start;
			flex-direction: column;
			gap: 0.75rem;
		}
		.history-item {
			grid-template-columns: 1.25rem minmax(0, 1fr);
			grid-template-rows: auto auto;
			gap: 0.5rem 0.75rem;
			padding-bottom: 1.5rem;
		}
		.history-time {
			grid-column: 2;
			grid-row: 1;
			justify-self: start;
			padding-top: 0;
		}
		.history-rail {
			grid-column: 1;
			grid-row: 1 / span 2;
			justify-content: flex-start;
			padding-inline-start: 0.25rem;
		}
		.history-item:not(:last-child) .history-rail::after {
			inset-block: 0.5rem -1.5rem;
		}
		.history-item :global(.history-event) {
			grid-column: 2;
			grid-row: 2;
		}
	}
</style>
