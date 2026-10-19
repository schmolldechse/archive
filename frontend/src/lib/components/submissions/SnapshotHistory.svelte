<script lang="ts">
	import { page } from "$app/state";
	import type { SnapshotHistoryItemResponse, SnapshotHistoryResponse } from "$api";
	import { historyLabel, historyReason } from "$lib/submission-history";
	import { pageHref } from "$lib/admin/navigation";
	import ArchiveTimestamp from "$lib/components/site/ArchiveTimestamp.svelte";
	import AdminPagination from "$lib/components/admin/AdminPagination.svelte";

	let { history }: { history: SnapshotHistoryResponse } = $props();

	/** Groups versioned lifecycle events for quick scanning without hiding their full labels. */
	function historyCategory(item: SnapshotHistoryItemResponse): string {
		if (item.content.version !== 1) return "Record";
		switch (item.content.type) {
			case "snapshot.submitted":
			case "snapshot.resubmitted":
				return "Submission";
			case "snapshot.stored":
			case "snapshot.processing_finished":
				return "Capture";
			case "snapshot.moderated":
				return "Moderation";
			default:
				return "Record";
		}
	}

	/** Applies restrained semantic emphasis while leaving the event label as the primary signal. */
	function historyTone(item: SnapshotHistoryItemResponse): string {
		if (item.content.version !== 1) return "neutral";
		if (item.content.type === "snapshot.stored") return "success";
		if (item.content.type === "snapshot.processing_finished") return "warning";
		if (item.content.type === "snapshot.submitted" || item.content.type === "snapshot.resubmitted") {
			return "register";
		}
		if (item.content.type === "snapshot.moderated") {
			const data = item.content.data;
			if (data !== null && typeof data === "object" && !Array.isArray(data)) {
				const status = (data as Record<string, unknown>).status;
				if (status === "APPROVED") return "success";
				if (status === "REJECTED") return "critical";
			}
		}
		return "neutral";
	}

	const total = $derived(Number(history.total));
</script>

<section class="snapshot-history" aria-labelledby="snapshot-history-title">
	<header class="history-header">
		<div>
			<p class="history-kicker">Record log</p>
			<h2 id="snapshot-history-title">Snapshot history</h2>
		</div>
		<p class="history-count" aria-label={`${total} history events, newest first`}>
			<strong>{total}</strong>
			<span>{total === 1 ? "event" : "events"}</span>
			<span class="history-count__order">Newest first</span>
		</p>
	</header>

	{#if !history.items.length}
		<p class="history-empty">No history events are recorded for this snapshot.</p>
	{:else}
		<ol class="history-list">
			{#each history.items as item (item.sequence)}
				<li class="history-item" data-tone={historyTone(item)}>
					<div class="history-time"><ArchiveTimestamp value={item.createdAt} density="compact" /></div>
					<div class="history-rail" aria-hidden="true"><span class="history-mark"></span></div>
					<article class="history-event">
						<p class="history-category">{historyCategory(item)}</p>
						<h3>{historyLabel(item)}</h3>
						{#if item.actor}
							<p class="history-actor">By @{item.actor.handle}<span>{item.actor.role}</span></p>
						{/if}
						{#if historyReason(item)}
							<div class="history-note">
								<p class="history-note__label">Review note</p>
								<p class="history-note__text">{historyReason(item)}</p>
							</div>
						{/if}
					</article>
				</li>
			{/each}
		</ol>
		{#if total > Number(history.pageSize)}
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
	.history-count__order {
		border-inline-start: 1px solid var(--border-trace);
		margin-inline-start: 0.5rem;
		padding-inline-start: 0.75rem;
	}
	.history-list {
		list-style: none;
		margin: 0;
		padding: 0;
	}
	.history-item {
		display: grid;
		grid-template-columns: minmax(10rem, 0.8fr) 1.5rem minmax(0, 2fr);
		gap: 0 1.25rem;
		position: relative;
		padding-bottom: 2rem;
	}
	.history-item:last-child {
		padding-bottom: 0.5rem;
	}
	.history-time {
		grid-column: 1;
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
		inset-block: 0.875rem -2rem;
		border-inline-start: 1px solid var(--border-trace);
		content: "";
	}
	.history-mark {
		position: relative;
		z-index: 1;
		width: 0.75rem;
		height: 0.75rem;
		margin-top: 0.25rem;
		border: 2px solid var(--border-trace);
		border-radius: 50%;
		background: var(--reading-room);
	}
	.history-item[data-tone="register"] .history-mark {
		border-color: var(--register-mark);
	}
	.history-item[data-tone="success"] .history-mark {
		border-color: var(--preservation-green);
		background: var(--preservation-green);
	}
	.history-item[data-tone="warning"] .history-mark {
		border-color: var(--warning-ochre);
		background: var(--warning-ochre);
	}
	.history-item[data-tone="critical"] .history-mark {
		border-color: var(--time-marker);
		background: var(--time-marker);
	}
	.history-event {
		grid-column: 3;
		min-width: 0;
		padding-bottom: 1.5rem;
		border-bottom: 1px solid var(--border-trace);
	}
	.history-item:last-child .history-event {
		border-bottom: 0;
		padding-bottom: 0;
	}
	.history-category {
		margin: 0 0 0.375rem;
		color: var(--marginal-note);
		font-size: 0.6875rem;
		font-weight: 700;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	.history-item[data-tone="register"] .history-category {
		color: var(--register-mark);
	}
	.history-item[data-tone="success"] .history-category {
		color: var(--preservation-green);
	}
	.history-item[data-tone="warning"] .history-category,
	.history-item[data-tone="critical"] .history-category {
		color: var(--time-marker);
	}
	h3 {
		margin: 0;
		font: 500 1.375rem/1.3 var(--font-editorial);
		letter-spacing: -0.005em;
	}
	.history-actor {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.375rem 0.75rem;
		margin: 0.5rem 0 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
	.history-actor > span {
		border-inline-start: 1px solid var(--border-trace);
		padding-inline-start: 0.75rem;
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.04em;
		text-transform: uppercase;
	}
	.history-note {
		max-width: 72ch;
		margin-top: 1rem;
		border-inline-start: 2px solid var(--border-trace);
		padding: 0.625rem 0 0.625rem 1rem;
	}
	.history-note__label {
		margin: 0 0 0.25rem;
		color: var(--marginal-note);
		font-size: 0.6875rem;
		font-weight: 700;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	.history-note__text {
		margin: 0;
		font-size: 0.9375rem;
		line-height: 1.55;
		white-space: pre-wrap;
		overflow-wrap: anywhere;
	}
	.history-empty {
		margin: 0;
		border-block: 1px solid var(--border-trace);
		padding: 1.5rem 0;
		color: var(--marginal-note);
	}
	.history-pagination {
		border-top: 1px solid var(--border-trace);
		margin-top: 1.25rem;
		padding-top: 1.25rem;
	}
	@media (max-width: 48rem) {
		.history-header {
			align-items: start;
			flex-direction: column;
			gap: 0.75rem;
		}
		.history-count__order {
			margin-inline-start: 0.375rem;
			padding-inline-start: 0.625rem;
		}
		.history-item {
			grid-template-columns: 1.25rem minmax(0, 1fr);
			grid-template-rows: auto auto;
			gap: 0.5rem 0.75rem;
			padding-bottom: 1.5rem;
		}
		.history-item:last-child {
			padding-bottom: 0.25rem;
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
			inset-block: 0.875rem -1.5rem;
		}
		.history-event {
			grid-column: 2;
			grid-row: 2;
			padding-bottom: 1.25rem;
		}
		h3 {
			font-size: 1.25rem;
		}
	}
</style>
