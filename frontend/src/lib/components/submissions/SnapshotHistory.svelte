<script lang="ts">
	import { page } from "$app/state";
	import type { SnapshotHistoryResponse } from "$api";
	import { historyLabel, historyReason } from "$lib/submission-history";
	import { pageHref } from "$lib/admin/navigation";
	import LocalTimestamp from "$lib/components/site/LocalTimestamp.svelte";
	import AdminPagination from "$lib/components/admin/AdminPagination.svelte";
	let { history }: { history: SnapshotHistoryResponse } = $props();
</script>

<section class="snapshot-history" aria-labelledby="snapshot-history-title">
	<h2 id="snapshot-history-title">Snapshot history</h2>
	{#if !history.items.length}<p class="history-empty">No history entries are available.</p>{:else}
		<ol>
			{#each history.items as item (item.sequence)}
				<li>
					<span class="history-mark" aria-hidden="true"></span>
					<div>
						<h3>{historyLabel(item)}</h3>
						<p class="history-meta">
							<LocalTimestamp value={item.createdAt} />{#if item.actor}<span>@{item.actor.handle} · {item.actor.role}</span
								>{/if}
						</p>
						{#if historyReason(item)}<p class="history-reason">{historyReason(item)}</p>{/if}
					</div>
				</li>
			{/each}
		</ol>
		{#if Number(history.total) > Number(history.pageSize)}<AdminPagination
				total={Number(history.total)}
				pageSize={Number(history.pageSize)}
				currentPage={Number(history.page)}
				href={(value) => pageHref(page.url, "historyPage", value)}
				label="Snapshot history pages"
			/>{/if}
	{/if}
</section>

<style>
	h2 {
		margin: 0 0 1.5rem;
		font-size: 1.75rem;
		font-weight: 500;
	}
	ol {
		list-style: none;
		margin: 0 0 1rem 0.25rem;
		padding: 0;
	}
	li {
		position: relative;
		display: grid;
		grid-template-columns: 1rem minmax(0, 1fr);
		gap: 1rem;
		padding: 0 0 1.5rem;
	}
	li:not(:last-child)::before {
		content: "";
		position: absolute;
		top: 0.375rem;
		bottom: 0;
		left: 0.3125rem;
		border-left: 1px solid var(--border-trace);
	}
	.history-mark {
		position: relative;
		width: 0.6875rem;
		height: 0.6875rem;
		margin-top: 0.375rem;
		background: var(--reading-room);
		border: 2px solid var(--border-trace);
		border-radius: 2px;
	}
	h3 {
		margin: 0;
		font: 600 1rem/1.5 var(--font-interface);
	}
	.history-meta {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem 1rem;
		margin: 0.375rem 0 0;
		font-size: 0.75rem;
		color: var(--marginal-note);
		overflow-wrap: anywhere;
	}
	.history-reason {
		margin: 0.75rem 0 0;
		max-width: 65ch;
		white-space: pre-wrap;
		overflow-wrap: anywhere;
		font-size: 0.9375rem;
	}
	.history-empty {
		color: var(--marginal-note);
	}
</style>
