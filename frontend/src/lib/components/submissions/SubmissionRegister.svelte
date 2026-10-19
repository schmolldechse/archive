<script lang="ts">
	import { page } from "$app/state";
	import { processingLabel } from "$lib/submission-status";
	import type { SnapshotSubmissionListResponse } from "$api";
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import FileSearch from "@lucide/svelte/icons/file-search";
	import Button from "$lib/components/ui/Button.svelte";
	import AdminPagination from "$lib/components/admin/AdminPagination.svelte";
	import ArchiveTimestamp from "$lib/components/site/ArchiveTimestamp.svelte";
	import SubmissionFilters from "./SubmissionFilters.svelte";
	import SnapshotModerationStatus from "./SnapshotModerationStatus.svelte";
	import { pageHref } from "$lib/admin/navigation";
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
					: "Your submitted snapshots and their review status will appear here."}
			</p>
			{#if !filters.q && !filters.status}<Button href="/archive/new">Archive a page</Button>{/if}
		</div>
	{:else}
		<ol class="submission-list">
			{#each submissions.items as item (item.id)}
				<li>
					<div class="submission-record">
						<a class="submission-title" href={`${base}/${item.id}`}>{item.title}</a>
						{#if item.sourceUrl}<p class="submission-source">{item.sourceUrl}</p>{/if}
						<p class="submission-meta">
							<span>{item.createdBy ? `@${item.createdBy.handle}` : "Anonymous"}</span><span
								>Submitted <ArchiveTimestamp value={item.createdAt} /></span
							>
						</p>
					</div>
					<div class="submission-state">
						<SnapshotModerationStatus status={item.moderationStatus} />
						{#if item.processingStatus !== "COMPLETED"}<span class="capture-state"
								>{processingLabel(item.processingStatus)}</span
							>{/if}
					</div>
					<Button href={`${base}/${item.id}`} variant="text">View submission<ArrowRight aria-hidden="true" /></Button>
				</li>
			{/each}
		</ol>
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
		text-transform: uppercase;
		letter-spacing: 0.08em;
		font-size: 0.75rem;
		font-weight: 600;
		color: var(--marginal-note);
	}
	h1 {
		margin: 0;
		font-size: clamp(2.25rem, 4.5vw, 3.5rem);
		font-weight: 500;
		line-height: 1.12;
		letter-spacing: -0.025em;
	}
	.register-heading > p:last-child {
		color: var(--marginal-note);
		margin: 0.75rem 0 0;
		max-width: 42rem;
	}
	.register-caption {
		display: flex;
		flex-wrap: wrap;
		justify-content: space-between;
		gap: 0.5rem;
		align-items: baseline;
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
	.submission-list {
		list-style: none;
		padding: 0;
		margin: 0;
		border-top: 1px solid var(--border-trace);
	}
	li {
		display: grid;
		grid-template-columns: minmax(0, 1fr) 11rem auto;
		align-items: center;
		gap: 1rem;
		padding-block: 1.5rem;
		border-bottom: 1px solid var(--border-trace);
	}
	.submission-record {
		min-width: 0;
	}
	.submission-title {
		font: 500 1.5rem/1.3 var(--font-editorial);
		color: var(--reading-ink);
		overflow-wrap: anywhere;
		text-decoration: none;
	}
	.submission-title:hover {
		text-decoration: underline;
	}
	.submission-source {
		margin: 0.5rem 0;
		font: 400 0.8125rem/1.5 var(--font-record);
		overflow-wrap: anywhere;
		color: var(--marginal-note);
	}
	.submission-meta {
		display: flex;
		flex-wrap: wrap;
		gap: 0.25rem 1rem;
		margin: 0.375rem 0 0;
		font-size: 0.75rem;
		color: var(--marginal-note);
	}
	.submission-state {
		display: grid;
		gap: 0.5rem;
	}
	.capture-state {
		font-size: 0.8125rem;
		color: var(--marginal-note);
	}
	.submission-empty {
		padding: 3rem 0;
		max-width: 38rem;
	}
	.submission-empty > :global(svg) {
		color: var(--marginal-note);
		width: 1.75rem;
		height: 1.75rem;
	}
	.submission-empty h3 {
		font-size: 1.25rem;
		margin: 1rem 0 0;
	}
	.submission-empty p {
		color: var(--marginal-note);
	}
	.register-pagination {
		padding-block: 1.5rem;
	}
	@media (max-width: 60rem) {
		li {
			grid-template-columns: minmax(0, 1fr) auto;
		}
		.submission-state {
			grid-column: 1;
		}
		li > :global(.button) {
			grid-column: 2;
			grid-row: 2;
		}
	}
	@media (max-width: 42rem) {
		.register-heading {
			padding-top: 2rem;
		}
		li {
			grid-template-columns: 1fr;
		}
		li > :global(.button) {
			grid-column: 1;
			grid-row: auto;
			justify-self: start;
		}
	}
</style>
