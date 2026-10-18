<script lang="ts">
	import ArrowLeftRight from "@lucide/svelte/icons/arrow-left-right";
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import type { SnapshotSubmissionSummaryResponse } from "$api";
	import ArchiveAttribution from "$lib/components/archive/ArchiveAttribution.svelte";
	import LocalTimestamp from "$lib/components/site/LocalTimestamp.svelte";
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
	import { processingLabel } from "$lib/submission-status";
	import SnapshotModerationStatus from "./SnapshotModerationStatus.svelte";

	let { items, base }: { items: SnapshotSubmissionSummaryResponse[]; base: string } = $props();

	function captureLabel(item: SnapshotSubmissionSummaryResponse): string {
		if (item.processingStatus !== "COMPLETED") return processingLabel(item.processingStatus);
		if (item.quality === "COMPLETE") return "Complete capture";
		if (item.quality === "INCOMPLETE") return "Incomplete capture";
		return "Capture complete";
	}
</script>

<p class="table-scroll-hint"><ArrowLeftRight aria-hidden="true" />Scroll to view all review details.</p>
<TableViewport aria-labelledby="submission-register-title">
	<TableRoot class="snapshot-review-table">
		<TableCaption class="sr-only">Snapshots in the moderation queue</TableCaption>
		<TableHeader>
			<TableRow>
				<TableHead>Snapshot</TableHead>
				<TableHead class="submitter-column">Submitted by</TableHead>
				<TableHead class="requested-column">Review requested</TableHead>
				<TableHead class="moderation-column">Moderation</TableHead>
				<TableHead class="capture-column">Capture</TableHead>
				<TableHead class="action-column" align="end">Action</TableHead>
			</TableRow>
		</TableHeader>
		<TableBody>
			{#each items as item (item.id)}
				{@const href = `${base}/${item.id}`}
				<TableRow>
					<TableCell class="snapshot-cell" data-label="Snapshot">
						<a class="snapshot-title" {href}>{item.title}</a>
						{#if item.sourceUrl}<a class="snapshot-source" href={item.sourceUrl} target="_blank" rel="noreferrer"
								>{item.sourceUrl}</a
							>{/if}
						<div class="snapshot-origin">
							<span>{item.sourceType}</span><span>Submitted <LocalTimestamp value={item.createdAt} /></span>
						</div>
					</TableCell>
					<TableCell class="submitter-cell" data-label="Submitted by">
						<ArchiveAttribution creator={item.createdBy} layout="compact" label={null} />
					</TableCell>
					<TableCell class="requested-cell" data-label="Review requested" valueKind="record"
						><LocalTimestamp value={item.moderationUpdatedAt} /></TableCell
					>
					<TableCell class="moderation-cell" data-label="Moderation">
						<SnapshotModerationStatus status={item.moderationStatus} />
					</TableCell>
					<TableCell class="capture-cell" data-label="Capture">
						<Badge
							variant={item.processingStatus === "COMPLETED" ? (item.quality === "COMPLETE" ? "success" : "warning") : "info"}
							size="compact">{captureLabel(item)}</Badge
						>
					</TableCell>
					<TableCell class="action-cell" data-label="Action" align="end">
						<Button {href} variant="text" class="review-action" aria-label={`Review ${item.title}`}
							>Review <ArrowRight aria-hidden="true" /></Button
						>
					</TableCell>
				</TableRow>
			{/each}
		</TableBody>
	</TableRoot>
</TableViewport>

<style>
	:global(.snapshot-review-table) {
		min-width: 68rem;
		table-layout: fixed;
	}
	:global(.snapshot-review-table [data-table-header]) {
		border-block: 1px solid var(--border-trace);
	}
	:global(.snapshot-review-table [data-table-head]) {
		color: var(--marginal-note);
		font-size: 0.75rem;
		letter-spacing: 0.06em;
		text-transform: uppercase;
	}
	:global(.snapshot-review-table [data-table-cell]) {
		padding-block: 1.25rem;
		vertical-align: top;
	}
	:global(.snapshot-review-table .submitter-column) {
		width: 13rem;
	}
	:global(.snapshot-review-table .requested-column) {
		width: 11.5rem;
	}
	:global(.snapshot-review-table .moderation-column) {
		width: 10.5rem;
	}
	:global(.snapshot-review-table .capture-column) {
		width: 10.5rem;
	}
	:global(.snapshot-review-table .action-column) {
		width: 10rem;
	}
	:global(.snapshot-review-table .review-action) {
		white-space: nowrap;
	}
	.snapshot-title {
		display: block;
		width: fit-content;
		max-width: 100%;
		color: var(--reading-ink);
		font-family: var(--font-editorial);
		font-size: 1.25rem;
		font-weight: 500;
		line-height: 1.25;
		text-decoration: none;
		overflow-wrap: anywhere;
	}
	.snapshot-title:hover {
		color: var(--register-mark);
		text-decoration: underline;
		text-underline-offset: 0.16em;
	}
	.snapshot-source {
		display: -webkit-box;
		max-width: 36rem;
		margin-top: 0.375rem;
		overflow: hidden;
		-webkit-box-orient: vertical;
		-webkit-line-clamp: 2;
		line-clamp: 2;
		color: var(--register-mark);
		font-family: var(--font-record);
		font-size: 0.75rem;
		line-height: 1.45;
		overflow-wrap: anywhere;
	}
	.snapshot-origin {
		display: flex;
		flex-wrap: wrap;
		gap: 0.25rem 0.75rem;
		margin-top: 0.625rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
	}
	.snapshot-origin > span:first-child {
		font-family: var(--font-record);
		font-weight: 500;
		letter-spacing: 0.04em;
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
	@media (max-width: 64rem) {
		.table-scroll-hint {
			display: flex;
		}
	}
	@media (max-width: 42rem) {
		.table-scroll-hint {
			display: none;
		}
		:global(.snapshot-review-table) {
			min-width: 0;
			table-layout: auto;
		}
		:global(.snapshot-review-table [data-table-header]) {
			position: absolute;
			width: 1px;
			height: 1px;
			margin: -1px;
			overflow: hidden;
			clip-path: inset(50%);
			white-space: nowrap;
		}
		:global(.snapshot-review-table tbody) {
			display: block;
		}
		:global(.snapshot-review-table tbody [data-table-row]) {
			display: grid;
			grid-template-columns: repeat(2, minmax(0, 1fr));
			padding-block: 0.75rem;
		}
		:global(.snapshot-review-table [data-table-cell]) {
			display: block;
			min-width: 0;
			padding: 0.625rem 0.75rem;
			text-align: left;
		}
		:global(.snapshot-review-table [data-table-cell]::before) {
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
		:global(.snapshot-review-table .snapshot-cell),
		:global(.snapshot-review-table .submitter-cell),
		:global(.snapshot-review-table .action-cell) {
			grid-column: 1 / -1;
		}
		:global(.snapshot-review-table .snapshot-title) {
			font-size: 1.125rem;
		}
		:global(.snapshot-review-table .action-cell) {
			padding-top: 0.25rem;
		}
		:global(.snapshot-review-table .action-cell::before) {
			display: none;
		}
		:global(.snapshot-review-table .review-action) {
			width: 100%;
			border: 1px solid var(--border-trace);
			text-decoration: none;
		}
	}
</style>
