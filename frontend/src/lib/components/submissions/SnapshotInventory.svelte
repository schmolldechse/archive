<script lang="ts">
	import ArrowLeftRight from "@lucide/svelte/icons/arrow-left-right";
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import type { SnapshotSubmissionSummaryResponse } from "$api";
	import ArchiveAttribution from "$lib/components/archive/ArchiveAttribution.svelte";
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
	import { processingLabel } from "$lib/submission-status";
	import SnapshotModerationStatus from "./SnapshotModerationStatus.svelte";

	let { items }: { items: SnapshotSubmissionSummaryResponse[] } = $props();

	/** Turns capture and processing fields into one concise, accessible state label. */
	function captureLabel(item: SnapshotSubmissionSummaryResponse): string {
		if (item.processingStatus !== "COMPLETED") return processingLabel(item.processingStatus);
		if (item.quality === "COMPLETE") return "Fully captured";
		if (item.quality === "INCOMPLETE") return "Incomplete capture";
		return "Capture stored";
	}

	/** Formats backend integer strings exactly, including values above JavaScript's safe integer limit. */
	function formatBytes(value: string | number | null): string {
		if (value === null) return "Not available";
		let bytes: bigint;
		try {
			bytes = BigInt(value);
		} catch {
			return "Not available";
		}
		if (bytes < 0n) return "Not available";
		if (bytes < 1024n) return `${bytes.toLocaleString("en")} B`;
		const units = ["KiB", "MiB", "GiB", "TiB"];
		let divisor = 1024n;
		let unit = 0;
		while (bytes >= divisor * 1024n && unit < units.length - 1) {
			divisor *= 1024n;
			unit += 1;
		}
		const tenths = (bytes * 10n + divisor / 2n) / divisor;
		const decimal = tenths % 10n;
		const size = tenths / 10n;
		return `${size.toLocaleString("en")}${decimal === 0n ? "" : `.${decimal}`} ${units[unit]}`;
	}

	/** Keeps missing capture metrics explicit rather than implying a zero-sized capture. */
	function metric(value: string | number | null, suffix = ""): string {
		if (value === null) return "Not available";
		try {
			return `${BigInt(value).toLocaleString("en")}${suffix}`;
		} catch {
			return "Not available";
		}
	}

	function captureVariant(item: SnapshotSubmissionSummaryResponse): "info" | "success" | "warning" | "critical" {
		if (item.processingStatus !== "COMPLETED") {
			return item.processingStatus === "FAILED" || item.processingStatus === "ABORTED" ? "critical" : "info";
		}
		return item.quality === "COMPLETE" ? "success" : "warning";
	}
</script>

<p class="table-scroll-hint"><ArrowLeftRight aria-hidden="true" />Scroll to view all snapshot details.</p>
<TableViewport aria-labelledby="snapshot-register-title">
	<TableRoot class="snapshot-inventory-table">
		<TableCaption class="sr-only">Snapshot records with their capture and moderation states</TableCaption>
		<TableHeader>
			<TableRow>
				<TableHead>Snapshot</TableHead>
				<TableHead class="submitted-column">Submitted</TableHead>
				<TableHead class="capture-column">Capture status</TableHead>
				<TableHead class="data-column">Archived data</TableHead>
				<TableHead class="moderation-column">Moderation</TableHead>
				<TableHead class="action-column" align="end">Action</TableHead>
			</TableRow>
		</TableHeader>
		<TableBody>
			{#each items as item (item.id)}
				<TableRow>
					<TableCell class="snapshot-cell" data-label="Snapshot">
						<a class="snapshot-title" href={`/admin/snapshots/${item.id}`}>{item.title}</a>
						{#if item.sourceUrl}
							<a class="snapshot-source" href={item.sourceUrl} target="_blank" rel="noreferrer">{item.sourceUrl}</a>
						{/if}
						<p class="snapshot-origin">{item.sourceType}</p>
					</TableCell>
					<TableCell class="submitted-cell" data-label="Submitted">
						<ArchiveAttribution creator={item.createdBy} layout="compact" label={null} />
						<p class="record-time"><ArchiveTimestamp value={item.createdAt} density="compact" /></p>
					</TableCell>
					<TableCell class="capture-cell" data-label="Capture status">
						<Badge variant={captureVariant(item)} size="compact">{captureLabel(item)}</Badge>
						{#if item.captureCompletedAt}
							<p class="record-time"><ArchiveTimestamp value={item.captureCompletedAt} density="compact" /></p>
						{/if}
					</TableCell>
					<TableCell class="data-cell" data-label="Archived data">
						<dl class="capture-metrics">
							<div>
								<dt>Resources</dt>
								<dd>{metric(item.resourceCount)}</dd>
							</div>
							<div>
								<dt>Storage</dt>
								<dd>{formatBytes(item.storageBytes)}</dd>
							</div>
							<div>
								<dt>Duration</dt>
								<dd>{metric(item.durationMilliseconds, " ms")}</dd>
							</div>
						</dl>
					</TableCell>
					<TableCell class="moderation-cell" data-label="Moderation">
						<SnapshotModerationStatus status={item.moderationStatus} />
						<p class="record-time"><ArchiveTimestamp value={item.moderationUpdatedAt} density="compact" /></p>
					</TableCell>
					<TableCell class="action-cell" data-label="Action" align="end">
						<Button
							href={"/admin/snapshots/" + item.id}
							variant="text"
							class="record-action"
							aria-label={"View record: " + item.title}
						>
							View record<ArrowRight aria-hidden="true" />
						</Button>
					</TableCell>
				</TableRow>
			{/each}
		</TableBody>
	</TableRoot>
</TableViewport>

<style>
	:global(.snapshot-inventory-table) {
		min-width: 77rem;
		table-layout: fixed;
	}
	:global(.snapshot-inventory-table [data-table-header]) {
		border-block: 1px solid var(--border-trace);
	}
	:global(.snapshot-inventory-table [data-table-head]) {
		color: var(--marginal-note);
		font-size: 0.75rem;
		letter-spacing: 0.06em;
		text-transform: uppercase;
	}
	:global(.snapshot-inventory-table [data-table-cell]) {
		padding-block: 1.125rem;
		vertical-align: top;
	}
	:global(.snapshot-inventory-table .submitted-column) {
		width: 12rem;
	}
	:global(.snapshot-inventory-table .capture-column) {
		width: 12rem;
	}
	:global(.snapshot-inventory-table .data-column) {
		width: 12rem;
	}
	:global(.snapshot-inventory-table .moderation-column) {
		width: 12rem;
	}
	:global(.snapshot-inventory-table .action-column) {
		width: 9rem;
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
		display: block;
		max-width: 35rem;
		margin-top: 0.375rem;
		color: var(--register-mark);
		font-family: var(--font-record);
		font-size: 0.75rem;
		line-height: 1.45;
		overflow-wrap: anywhere;
		word-break: break-word;
	}
	.snapshot-origin {
		margin: 0.5rem 0 0;
		color: var(--marginal-note);
		font-size: 0.75rem;
		line-height: 1.45;
	}
	.record-time {
		margin: 0.5rem 0 0;
	}
	.snapshot-origin {
		font-family: var(--font-record);
		font-weight: 500;
		letter-spacing: 0.04em;
	}
	.capture-metrics {
		display: grid;
		gap: 0.25rem;
		margin: 0;
	}
	.capture-metrics > div {
		display: flex;
		flex-wrap: wrap;
		justify-content: space-between;
		gap: 0.25rem 0.5rem;
	}
	.capture-metrics dt,
	.capture-metrics dd {
		margin: 0;
		font-size: 0.75rem;
		line-height: 1.4;
	}
	.capture-metrics dt {
		color: var(--marginal-note);
	}
	.capture-metrics dd {
		color: var(--reading-ink);
		font-family: var(--font-record);
		font-variant-numeric: tabular-nums;
		text-align: right;
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
	@media (max-width: 82rem) {
		.table-scroll-hint {
			display: flex;
		}
	}
	@media (max-width: 48rem) {
		.table-scroll-hint {
			display: none;
		}
		:global(.snapshot-inventory-table) {
			min-width: 0;
			table-layout: auto;
		}
		:global(.snapshot-inventory-table [data-table-header]) {
			position: absolute;
			width: 1px;
			height: 1px;
			margin: -1px;
			overflow: hidden;
			clip-path: inset(50%);
			white-space: nowrap;
		}
		:global(.snapshot-inventory-table tbody) {
			display: block;
		}
		:global(.snapshot-inventory-table tbody [data-table-row]) {
			display: grid;
			grid-template-columns: repeat(2, minmax(0, 1fr));
			padding-block: 0.5rem;
		}
		:global(.snapshot-inventory-table [data-table-cell]) {
			display: block;
			min-width: 0;
			padding: 0.75rem;
			text-align: left;
		}
		:global(.snapshot-inventory-table [data-table-cell]::before) {
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
		:global(.snapshot-inventory-table .snapshot-cell),
		:global(.snapshot-inventory-table .submitted-cell),
		:global(.snapshot-inventory-table .action-cell) {
			grid-column: 1 / -1;
		}
		:global(.snapshot-inventory-table .snapshot-title) {
			font-size: 1.125rem;
		}
		:global(.snapshot-inventory-table .action-cell) {
			padding-top: 0.25rem;
		}
		:global(.snapshot-inventory-table .record-action) {
			width: 100%;
			min-height: 2.75rem;
			border: 1px solid var(--border-trace);
			text-decoration: none;
		}
	}
</style>
