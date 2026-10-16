<script lang="ts">
	import Info from "@lucide/svelte/icons/info";
	import X from "@lucide/svelte/icons/x";
	import type { SnapshotResponse } from "$api";
	import { DialogClose, DialogContent, DialogRoot, DialogTitle, DialogTrigger } from "$lib/components/ui/dialog";
	import CopySourceButton from "./CopySourceButton.svelte";
	import SnapshotMetadata from "./SnapshotMetadata.svelte";
	let { snapshot }: { snapshot: SnapshotResponse } = $props();
	const source = $derived(snapshot.sourceUrl);
</script>

<DialogRoot>
	<DialogTrigger class="snapshot-info-trigger" aria-label="Snapshot information" title="Snapshot information">
		<Info size={20} aria-hidden="true" />
	</DialogTrigger>
	<DialogContent class="snapshot-info-dialog" aria-describedby={undefined}>
		<div class="snapshot-info-heading">
			<DialogTitle>Snapshot information</DialogTitle>
			<DialogClose aria-label="Close snapshot information"><X size={20} aria-hidden="true" /></DialogClose>
		</div>
		<div class="snapshot-info-body">
			<h3>{snapshot.title}</h3>
			{#if source}
				<div class="snapshot-info-source">
					<a href={source} target="_blank" rel="noreferrer">{source}</a>
					<CopySourceButton value={source} />
				</div>
			{/if}
			<SnapshotMetadata {snapshot} includeIdentifier />
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
		padding: 1.5rem;
	}
	h3 {
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
	@media (max-width: 36rem) {
		.snapshot-info-heading,
		.snapshot-info-body {
			padding: 1rem;
		}
		h3 {
			font-size: 1.75rem;
		}
	}
</style>
