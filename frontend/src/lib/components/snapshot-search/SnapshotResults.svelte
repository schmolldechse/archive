<script lang="ts">
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import type { SnapshotListResponse, SnapshotResponse } from "$api";
	import ArchiveRecord from "$lib/components/archive/ArchiveRecord.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import Skeleton from "$lib/components/ui/Skeleton.svelte";
	import {
		PaginationEllipsis,
		PaginationList,
		PaginationNext,
		PaginationPage,
		PaginationPrevious,
		PaginationRoot
	} from "$lib/components/ui/pagination";
	import { pageSize, searchHref, type SearchState } from "./search-state";
	let {
		results,
		committed,
		selectedId,
		mobile,
		previewOpen,
		pending = false,
		error,
		invalid = false,
		onInspect,
		onCopy,
		onClear,
		onRetry,
		onPage
	}: {
		results: SnapshotListResponse | null;
		committed: SearchState;
		selectedId: string | null;
		mobile: boolean;
		previewOpen: boolean;
		pending?: boolean;
		error: string | null;
		invalid?: boolean;
		onInspect: (snapshot: SnapshotResponse, trigger: HTMLButtonElement) => void;
		onCopy: (snapshot: SnapshotResponse) => void;
		onClear: () => void;
		onRetry: () => void;
		onPage: (page: number) => void;
	} = $props();
</script>

<div class="results-column" data-component="snapshot-results" aria-busy={pending}>
	{#if pending}
		<div class="record-skeletons" aria-label="Loading snapshot records" role="status">
			{#each [1, 2, 3] as item (item)}<div><Skeleton class="record-skeleton" /><Skeleton class="record-skeleton" /></div>{/each}
		</div>
	{:else if invalid}
		<div class="message-state">
			<CircleAlert aria-hidden="true" />
			<h3>Review the search notation</h3>
			<p>Correct the highlighted search parameters and submit the form.</p>
			<Button variant="text" onclick={onClear}>Return to the full register</Button>
		</div>
	{:else if error}
		<div class="message-state">
			<CircleAlert aria-hidden="true" />
			<h3>The register is unavailable</h3>
			<p>{error}</p>
			<Button onclick={onRetry}>Try again</Button>
		</div>
	{:else if results?.items.length}
		<ol class="result-list" aria-label="Snapshot search results">
			{#each results.items as snapshot (snapshot.id)}
				<li>
					<ArchiveRecord
						{snapshot}
						selected={selectedId === snapshot.id}
						previewIsDialog={mobile}
						previewExpanded={previewOpen && selectedId === snapshot.id}
						{onInspect}
						{onCopy}
					/>
				</li>
			{/each}
		</ol>
		<PaginationRoot
			totalItems={Number(results.total)}
			{pageSize}
			page={committed.page}
			onPageChange={onPage}
			getHref={(page) => searchHref({ ...committed, page }, "snapshot-register")}
			label="Snapshot result pages"
			alwaysShow
		>
			{#snippet children({ items, range })}
				<div class="pagination-row">
					<span class="range">{range.start}–{range.end} of {range.total} snapshots</span><PaginationList
						><PaginationPrevious />
						{#each items as item (item.key)}{#if item.type === "page"}<PaginationPage {item} />{:else}<PaginationEllipsis
								/>{/if}{/each}<PaginationNext />
					</PaginationList>
				</div>
			{/snippet}
		</PaginationRoot>
	{:else}
		<div class="message-state">
			<div class="empty-register" aria-hidden="true"></div>
			<h3>No preserved states match this notation</h3>
			<p>Try fewer text terms, remove one required tag, or widen the capture period.</p>
			<Button variant="text" onclick={onClear}>Return to the full register</Button>
		</div>
	{/if}
</div>

<style>
	.results-column {
		min-width: 0;
	}
	.result-list {
		list-style: none;
		padding: 0;
		margin: 0;
	}
	.result-list li {
		margin: 0;
	}
	.pagination-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		flex-wrap: wrap;
		gap: 1rem;
		padding-block: 1.5rem;
	}
	.range {
		font-family: var(--font-record);
		font-size: 0.75rem;
		color: var(--marginal-note);
	}
	.message-state {
		border-bottom: 1px solid var(--border-trace);
		padding: 3rem 1rem;
	}
	.message-state > :global(svg) {
		width: 1.5rem;
		color: var(--time-marker);
	}
	.message-state h3 {
		font-size: 1.375rem;
		font-weight: 600;
		margin: 1rem 0;
	}
	.message-state p {
		color: var(--marginal-note);
		font-size: 1rem;
		max-width: 40rem;
	}
	.empty-register {
		width: 5rem;
		height: 2rem;
		border-block: 1px solid var(--border-trace);
		border-left: 3px solid var(--border-trace);
	}
	.record-skeletons > div {
		display: grid;
		gap: 1rem;
		padding: 2rem 1rem;
		border-bottom: 1px solid var(--border-trace);
	}
	:global(.record-skeleton) {
		height: 2rem;
	}
</style>
