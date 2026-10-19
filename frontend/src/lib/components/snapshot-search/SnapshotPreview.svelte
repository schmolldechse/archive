<script lang="ts">
	import X from "@lucide/svelte/icons/x";
	import type { SnapshotResponse } from "$api";
	import { DialogClose, DialogContent, DialogRoot, DialogTitle } from "$lib/components/ui/dialog";
	import SnapshotPreviewContent from "./SnapshotPreviewContent.svelte";
	let {
		snapshot,
		mobile,
		open,
		onOpenChange,
		returnFocus
	}: {
		snapshot: SnapshotResponse | null;
		mobile: boolean;
		open: boolean;
		onOpenChange: (open: boolean) => void;
		returnFocus: () => HTMLElement | null;
	} = $props();
</script>

{#if mobile}
	<DialogRoot {open} {onOpenChange}>
		{#if snapshot}<DialogContent
				id="snapshot-preview"
				size="large"
				class="snapshot-bottom-dialog"
				{returnFocus}
				aria-describedby={undefined}
			>
				<div class="dialog-heading">
					<DialogTitle>Snapshot preview</DialogTitle><DialogClose aria-label="Close snapshot preview"
						><X aria-hidden="true" /></DialogClose
					>
				</div>
				<SnapshotPreviewContent {snapshot} />
			</DialogContent>{/if}
	</DialogRoot>
{:else}
	<DialogRoot {open} {onOpenChange}>
		{#if snapshot}<DialogContent
				id="snapshot-preview"
				size="viewport"
				class="snapshot-desktop-dialog"
				{returnFocus}
				aria-describedby={undefined}
			>
				<div class="dialog-heading">
					<DialogTitle>Snapshot preview</DialogTitle><DialogClose aria-label="Close snapshot preview"
						><X aria-hidden="true" /></DialogClose
					>
				</div>
				<SnapshotPreviewContent {snapshot} layout="desktop" />
			</DialogContent>{/if}
	</DialogRoot>
{/if}

<style>
	:global([data-dialog-content].snapshot-bottom-dialog) {
		position: fixed;
		inset: auto 0 0;
		width: 100%;
		max-width: none;
		max-height: 100dvh;
		margin: 0;
		padding: 0 0 env(safe-area-inset-bottom);
		border-radius: 10px 10px 0 0;
		box-shadow: var(--overlay-shadow);
		overflow-y: auto;
	}
	:global([data-dialog-content][data-size="viewport"].snapshot-desktop-dialog) {
		position: fixed;
		inset: 0;
		width: min(90vw, 86rem);
		height: min(88dvh, 56rem);
		max-height: calc(100dvh - 2rem);
		flex-direction: column;
		margin: auto;
		overflow: hidden;
		padding: 0;
	}
	:global([data-dialog-content].snapshot-desktop-dialog[open]) {
		display: flex;
	}
	.dialog-heading {
		position: sticky;
		z-index: 1;
		top: 0;
		flex-shrink: 0;
		background: var(--reading-room);
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 1rem;
		padding: 1rem;
		border-bottom: 1px solid var(--border-trace);
	}
	.dialog-heading :global([data-dialog-title]) {
		font-size: 1.75rem;
	}
	.dialog-heading :global(button) {
		min-width: 44px;
		padding-inline: 0.5rem;
	}
</style>
