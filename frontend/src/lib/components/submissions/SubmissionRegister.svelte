<script lang="ts">
	import { page } from "$app/state";
	import type { SnapshotSubmissionListResponse } from "$api";
	import ArrowLeftRight from "@lucide/svelte/icons/arrow-left-right";
	import FileSearch from "@lucide/svelte/icons/file-search";
	import AdminPagination from "$lib/components/admin/AdminPagination.svelte";
	import ArchiveTimestamp from "$lib/components/site/ArchiveTimestamp.svelte";
	import Badge from "$lib/components/ui/Badge.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import {
		TableBody,
		TableCaption,
		TableCell,
		TableHead,
		TableHeader,
		TableRoot,
		TableRow,
		TableViewport
	} from "$lib/components/ui/table";
	import { pageHref } from "$lib/admin/navigation";
	import { captureLabel, captureVariant } from "$lib/submission-status";
	import SubmissionFilters from "./SubmissionFilters.svelte";
	import SnapshotModerationStatus from "./SnapshotModerationStatus.svelte";

	let {
		submissions,
		filters
	}: {
		submissions: SnapshotSubmissionListResponse;
		filters: { q: string; status: string };
	} = $props();

	const base = "/account/submissions";
	const total = $derived(Number(submissions.total));
</script>

<section class="submission-register" aria-labelledby="submission-register-title">
	<header class="register-heading">
		<p class="section-label">Your account</p>
		<h1 id="submission-register-title">My submissions</h1>
		<p>Follow your captures, review decisions and requests for another review.</p>
	</header>
	<SubmissionFilters {filters} resetHref={base} />
	<div class="register-caption">
		<h2>Submissions</h2>
		<p role="status">{total.toLocaleString()} {total === 1 ? "submission" : "submissions"}</p>
	</div>
	{#if !submissions.items.length}
		<div class="submission-empty">
			<FileSearch aria-hidden="true" />
			<h3>No submissions found</h3>
			<p>
				{filters.q || filters.status
					? "Try a different search or status."
					: "Your snapshots and their review status will appear here."}
			</p>
			{#if !filters.q && !filters.status}<Button href="/archive/new">Archive a page</Button>{/if}
		</div>
	{:else}
		<p class="table-scroll-hint"><ArrowLeftRight aria-hidden="true" />Scroll to view all submission details.</p>
		<TableViewport aria-labelledby="submission-register-title">
			<TableRoot class="submission-table">
				<TableCaption class="sr-only">Your snapshots with their capture and review status</TableCaption>
				<TableHeader>
					<TableRow>
						<TableHead>Snapshot</TableHead>
						<TableHead class="date-column">Date</TableHead>
						<TableHead class="capture-column">Capture</TableHead>
						<TableHead class="review-column">Review</TableHead>
					</TableRow>
				</TableHeader>
				<TableBody>
					{#each submissions.items as item (item.id)}
						<TableRow>
							<TableCell class="snapshot-cell" data-label="Snapshot">
								<a class="submission-title" href={`${base}/${item.id}`}>{item.title}</a>
								{#if item.sourceUrl}
									<a class="submission-source" href={item.sourceUrl} target="_blank" rel="noreferrer" title={item.sourceUrl}>
										{item.sourceUrl}
									</a>
								{/if}
								<p class="source-type">{item.sourceType}</p>
							</TableCell>
							<TableCell class="date-cell" data-label="Date">
								<ArchiveTimestamp value={item.createdAt} density="compact" />
							</TableCell>
							<TableCell class="capture-cell" data-label="Capture">
								<Badge variant={captureVariant(item.processingStatus, item.quality)} size="compact">
									{captureLabel(item.processingStatus, item.quality)}
								</Badge>
								{#if item.captureCompletedAt}
									<p class="record-time"><ArchiveTimestamp value={item.captureCompletedAt} density="compact" /></p>
								{/if}
							</TableCell>
							<TableCell class="review-cell" data-label="Review">
								<SnapshotModerationStatus status={item.moderationStatus} />
								<p class="record-time"><ArchiveTimestamp value={item.moderationUpdatedAt} density="compact" /></p>
							</TableCell>
						</TableRow>
					{/each}
				</TableBody>
			</TableRoot>
		</TableViewport>
		<div class="register-pagination">
			<AdminPagination
				{total}
				pageSize={Number(submissions.pageSize)}
				currentPage={Number(submissions.page)}
				href={(value) => pageHref(page.url, "page", value)}
				label="Submission pages"
			/>
		</div>
	{/if}
</section>

<style>
	.register-heading {
		padding-block: 2.5rem 2rem;
	}
	.section-label {
		margin: 0 0 0.75rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	h1 {
		margin: 0;
		font-size: clamp(2.25rem, 4.5vw, 3.5rem);
		font-weight: 500;
		line-height: 1.12;
		letter-spacing: -0.025em;
	}
	.register-heading > p:last-child {
		max-width: 42rem;
		margin: 0.75rem 0 0;
		color: var(--marginal-note);
	}
	.register-caption {
		display: flex;
		flex-wrap: wrap;
		align-items: baseline;
		justify-content: space-between;
		gap: 0.5rem;
		padding-top: 2rem;
	}
	h2 {
		margin: 0;
		font-size: 1.5rem;
		font-weight: 500;
	}
	.register-caption p {
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
	.submission-empty {
		max-width: 38rem;
		padding: 3rem 0;
	}
	.submission-empty > :global(svg) {
		width: 1.75rem;
		height: 1.75rem;
		color: var(--marginal-note);
	}
	.submission-empty h3 {
		margin: 1rem 0 0;
		font-size: 1.25rem;
	}
	.submission-empty p {
		color: var(--marginal-note);
	}
	.table-scroll-hint {
		display: none;
		align-items: center;
		gap: 0.5rem;
		margin: 0 0 0.25rem;
		color: var(--marginal-note);
		font-size: 0.8125rem;
	}
	.table-scroll-hint :global(svg) {
		width: 1rem;
		height: 1rem;
		flex: none;
	}
	:global(.submission-table) {
		min-width: 56rem;
		table-layout: fixed;
	}
	:global(.submission-table [data-table-header]) {
		border-block: 1px solid var(--border-trace);
	}
	:global(.submission-table [data-table-head]) {
		color: var(--marginal-note);
		font-size: 0.75rem;
		letter-spacing: 0.06em;
		text-transform: uppercase;
	}
	:global(.submission-table [data-table-cell]) {
		padding-block: 1rem;
		vertical-align: top;
	}
	:global(.submission-table .date-column) {
		width: 12rem;
	}
	:global(.submission-table .capture-column),
	:global(.submission-table .review-column) {
		width: 12rem;
	}
	.submission-title {
		display: block;
		width: fit-content;
		max-width: 100%;
		color: var(--reading-ink);
		font-family: var(--font-editorial);
		font-size: 1.25rem;
		font-weight: 500;
		line-height: 1.25;
		text-decoration: underline;
		text-decoration-thickness: 0.06em;
		text-underline-offset: 0.14em;
		overflow-wrap: anywhere;
	}
	.submission-title:hover {
		color: var(--register-mark);
		text-decoration: underline;
		text-underline-offset: 0.16em;
	}
	.submission-source {
		display: block;
		max-width: 36rem;
		margin-top: 0.375rem;
		color: var(--register-mark);
		font-family: var(--font-record);
		font-size: 0.75rem;
		line-height: 1.45;
		overflow-wrap: anywhere;
		word-break: break-word;
	}
	.source-type {
		margin: 0.5rem 0 0;
		color: var(--marginal-note);
		font-family: var(--font-record);
		font-size: 0.75rem;
		font-weight: 500;
		letter-spacing: 0.04em;
	}
	.record-time {
		margin: 0.5rem 0 0;
	}
	.register-pagination {
		padding-block: 1.5rem;
	}
	@media (max-width: 60rem) {
		.table-scroll-hint {
			display: flex;
		}
	}
	@media (max-width: 48rem) {
		.table-scroll-hint {
			display: none;
		}
		:global(.submission-table) {
			min-width: 0;
			table-layout: auto;
		}
		:global(.submission-table [data-table-header]) {
			position: absolute;
			width: 1px;
			height: 1px;
			margin: -1px;
			overflow: hidden;
			clip-path: inset(50%);
			white-space: nowrap;
		}
		:global(.submission-table tbody) {
			display: block;
		}
		:global(.submission-table tbody [data-table-row]) {
			display: grid;
			grid-template-columns: repeat(2, minmax(0, 1fr));
			padding-block: 0.5rem;
		}
		:global(.submission-table [data-table-cell]) {
			display: block;
			min-width: 0;
			padding: 0.75rem;
			text-align: left;
		}
		:global(.submission-table [data-table-cell]::before) {
			display: block;
			margin-bottom: 0.375rem;
			color: var(--marginal-note);
			font-family: var(--font-interface);
			font-size: 0.6875rem;
			font-weight: 600;
			letter-spacing: 0.06em;
			text-transform: uppercase;
			content: attr(data-label);
		}
		:global(.submission-table .snapshot-cell),
		:global(.submission-table .date-cell) {
			grid-column: 1 / -1;
		}
		:global(.submission-table .submission-title) {
			font-size: 1.125rem;
		}
	}
</style>
