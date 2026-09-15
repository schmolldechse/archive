<script lang="ts">
	import type { SnapshotListResponse, SnapshotResponse } from "$api";
	import CaptureTimeline from "./CaptureTimeline.svelte";
	import SnapshotPreview from "./SnapshotPreview.svelte";
	import SnapshotResults from "./SnapshotResults.svelte";
	import type { SearchState } from "./search-state";
	let {
		results,
		committed,
		snapshot,
		mobile,
		previewOpen,
		pending = false,
		error,
		invalid = false,
		onInspect,
		onCopy,
		onClear,
		onRetry,
		onPage,
		onPeriod,
		onPreviewOpenChange,
		returnFocus
	}: {
		results: SnapshotListResponse | null;
		committed: SearchState;
		snapshot: SnapshotResponse | null;
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
		onPeriod: (from: string, through: string) => void;
		onPreviewOpenChange: (open: boolean) => void;
		returnFocus: () => HTMLElement | null;
	} = $props();
</script>

<section class="capture-sequence" id="snapshot-register" aria-labelledby="register-title" data-component="capture-sequence">
	<div class="section-heading">
		<div>
			<p class="section-label">02 / Capture sequence</p>
			<h2 id="register-title" tabindex="-1">Matching snapshots</h2>
		</div>
		<p class="result-status" role="status" aria-live="polite">
			{pending ? "Loading register…" : invalid ? "Search parameters need correction" : error ? "Register unavailable" : ""}
		</p>
	</div>
	<CaptureTimeline distribution={results?.captureDistribution ?? null} {committed} {pending} {onPeriod} />
	<div class="workbench" class:mobile>
		<SnapshotResults
			{results}
			{committed}
			selectedId={snapshot?.id ?? null}
			{mobile}
			{previewOpen}
			{pending}
			{error}
			{invalid}
			{onInspect}
			{onCopy}
			{onClear}
			{onRetry}
			{onPage}
		/>
		<SnapshotPreview {snapshot} {mobile} open={previewOpen} onOpenChange={onPreviewOpenChange} {returnFocus} {onCopy} />
	</div>
</section>

<style>
	.capture-sequence {
		padding-block: 3rem;
	}
	.section-heading {
		display: flex;
		align-items: end;
		justify-content: space-between;
		flex-wrap: wrap;
		gap: 1.5rem;
		margin-bottom: 1.5rem;
	}
	.section-label {
		margin: 0 0 0.5rem;
		font-size: 0.75rem;
		color: var(--marginal-note);
		text-transform: uppercase;
		letter-spacing: 0.08em;
	}
	h2 {
		margin: 0;
		font-size: 2rem;
		font-weight: 500;
		line-height: 1.18;
		letter-spacing: -0.015em;
		scroll-margin-top: 1rem;
	}
	.result-status {
		font-size: 0.875rem;
		color: var(--marginal-note);
		margin: 0 0 0.75rem;
	}
	.result-status:empty {
		margin: 0;
	}
	.workbench {
		display: grid;
		grid-template-columns: minmax(0, 1fr) minmax(17rem, 0.45fr);
		gap: 2rem;
		margin-top: 2rem;
	}
	.workbench.mobile {
		grid-template-columns: minmax(0, 1fr);
	}
	@media (max-width: 72rem) {
		.workbench {
			gap: 1.5rem;
		}
	}
	@media (max-width: 57.499rem) {
		.workbench {
			grid-template-columns: minmax(0, 1fr);
		}
	}
	@media (max-width: 42rem) {
		.section-heading {
			display: grid;
			width: 100%;
		}
		h2 {
			font-size: 1.75rem;
		}
		.result-status {
			margin: 0;
		}
	}
</style>
