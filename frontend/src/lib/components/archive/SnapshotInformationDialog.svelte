<script lang="ts">
	import History from "@lucide/svelte/icons/history";
	import Info from "@lucide/svelte/icons/info";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import X from "@lucide/svelte/icons/x";
	import type { SnapshotHistoryResponse, SnapshotResponse } from "$api";
	import SnapshotHistory from "$lib/components/submissions/SnapshotHistory.svelte";
	import { DialogClose, DialogContent, DialogRoot, DialogTitle, DialogTrigger } from "$lib/components/ui/dialog";
	import { TabsList, TabsPanel, TabsRoot, TabsTrigger } from "$lib/components/ui/tabs";
	import CopySourceButton from "./CopySourceButton.svelte";
	import SnapshotMetadata from "./SnapshotMetadata.svelte";

	let {
		snapshot,
		history,
		historyUnavailable = false
	}: {
		snapshot: SnapshotResponse;
		history: SnapshotHistoryResponse;
		historyUnavailable?: boolean;
	} = $props();
	const source = $derived(snapshot.sourceUrl);
</script>

<DialogRoot>
	<DialogTrigger
		class="snapshot-info-trigger"
		aria-label="Snapshot information and history"
		title="Snapshot information"
	>
		<Info size={20} aria-hidden="true" />
	</DialogTrigger>
	<DialogContent class="snapshot-info-dialog" size="large" aria-describedby={undefined}>
		<div class="snapshot-info-heading">
			<DialogTitle>Snapshot record</DialogTitle>
			<DialogClose aria-label="Close snapshot record"><X size={20} aria-hidden="true" /></DialogClose>
		</div>
		<div class="snapshot-info-body">
			<TabsRoot value="info">
				<TabsList aria-label="Snapshot record sections">
					<TabsTrigger value="info"><Info aria-hidden="true" />Info</TabsTrigger>
					<TabsTrigger value="history"><History aria-hidden="true" />History</TabsTrigger>
				</TabsList>
				<TabsPanel value="info">
					<div class="snapshot-info-record">
						<h3>{snapshot.title}</h3>
						{#if source}
							<div class="snapshot-info-source">
								<a href={source} target="_blank" rel="noreferrer">{source}</a>
								<CopySourceButton value={source} />
							</div>
						{/if}
						<SnapshotMetadata {snapshot} />
					</div>
				</TabsPanel>
				<TabsPanel value="history">
					{#if historyUnavailable}
						<div class="supplementary-unavailable" role="status">
							<CircleAlert aria-hidden="true" />
							<div>
								<h3>History temporarily unavailable</h3>
								<p>The archived page remains available. Reload the page to try again.</p>
							</div>
						</div>
					{:else}
						<SnapshotHistory {history} title="Record history" paginated={false} />
					{/if}
				</TabsPanel>
			</TabsRoot>
		</div>
	</DialogContent>
</DialogRoot>

<style>
	:global(button.button.snapshot-info-trigger[data-dialog-trigger]) {
		width: 3rem;
		min-height: 3rem;
		flex: none;
		border-color: var(--border-trace);
		background: var(--reading-room);
		box-shadow: var(--overlay-shadow);
		padding: 0;
		color: var(--register-mark);
	}
	:global(button.button.snapshot-info-trigger[data-dialog-trigger]:hover),
	:global(button.button.snapshot-info-trigger[data-state="open"]) {
		border-color: var(--border-trace);
		background: var(--archive-layer);
		color: var(--reading-ink);
	}
	:global([data-dialog-content].snapshot-info-dialog) {
		position: fixed;
		inset: 0;
		margin: auto;
		max-height: calc(100dvh - 2rem);
		padding: 0;
		overflow: hidden;
	}
	:global([data-dialog-content].snapshot-info-dialog[open]) {
		display: flex;
		flex-direction: column;
	}
	.snapshot-info-heading {
		display: flex;
		flex-shrink: 0;
		align-items: center;
		justify-content: space-between;
		gap: 1rem;
		border-bottom: 1px solid var(--border-trace);
		padding: 1rem 1.5rem;
	}
	.snapshot-info-heading :global([data-dialog-title]) {
		font-size: 1.5rem;
		line-height: 1.2;
	}
	.snapshot-info-heading :global(button) {
		flex-shrink: 0;
		width: 44px;
		padding-inline: 0;
	}
	.snapshot-info-body {
		min-height: 0;
		overflow-y: auto;
		padding: 0 1.5rem 1.5rem;
	}
	.snapshot-info-body :global([data-tabs-list]) {
		position: sticky;
		z-index: 1;
		top: 0;
		background: var(--reading-room);
	}
	.snapshot-info-body :global([data-tabs-trigger]) {
		gap: 0.5rem;
	}
	.snapshot-info-body :global([data-tabs-trigger] svg) {
		width: 1rem;
		height: 1rem;
		stroke-width: 1.75;
	}
	.snapshot-info-record h3 {
		margin: 0;
		font-family: var(--font-editorial);
		font-size: 2rem;
		font-weight: 500;
		line-height: 1.2;
		overflow-wrap: anywhere;
	}
	.snapshot-info-source {
		display: flex;
		align-items: center;
		gap: 0.25rem;
		margin-block: 0.5rem 1.5rem;
	}
	.snapshot-info-source a {
		min-width: 0;
		flex: 1;
		font-family: var(--font-record);
		font-size: 0.8125rem;
		overflow-wrap: anywhere;
	}
	.supplementary-unavailable {
		display: flex;
		align-items: flex-start;
		gap: 0.75rem;
		border-inline-start: 3px solid var(--warning-ochre);
		background: color-mix(in srgb, var(--warning-ochre) 9%, transparent);
		padding: 1rem;
	}
	.supplementary-unavailable :global(svg) {
		width: 1.125rem;
		height: 1.125rem;
		flex: none;
		color: var(--warning-ochre);
	}
	.supplementary-unavailable h3 {
		margin: 0;
		font: 500 1rem/1.4 var(--font-editorial);
	}
	.supplementary-unavailable p {
		margin: 0.25rem 0 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
	@media (max-width: 36rem) {
		.snapshot-info-heading {
			padding: 1rem;
		}
		.snapshot-info-body {
			padding: 0 1rem 1rem;
		}
		.snapshot-info-record h3 {
			font-size: 1.75rem;
		}
	}
</style>
