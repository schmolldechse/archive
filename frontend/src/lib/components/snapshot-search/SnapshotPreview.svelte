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
		returnFocus,
		onCopy
	}: {
		snapshot: SnapshotResponse | null;
		mobile: boolean;
		open: boolean;
		onOpenChange: (open: boolean) => void;
		returnFocus: () => HTMLElement | null;
		onCopy: (snapshot: SnapshotResponse) => void;
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
					<DialogTitle>Snapshot details</DialogTitle><DialogClose aria-label="Close snapshot details"
						><X aria-hidden="true" /></DialogClose
					>
				</div>
				<SnapshotPreviewContent {snapshot} {onCopy} />
			</DialogContent>{/if}
	</DialogRoot>
{:else}
	<aside id="snapshot-preview" class="preview-panel" aria-label="Snapshot preview">
		<SnapshotPreviewContent {snapshot} {onCopy} />
	</aside>
{/if}

<style>
	.preview-panel {
		min-width: 0;
		align-self: start;
		position: sticky;
		top: 1rem;
		border: 1px solid var(--border-trace);
		border-radius: 10px;
		overflow: hidden;
		background: var(--reading-room);
	}
	:global([data-dialog-content].snapshot-bottom-dialog) {
		position: fixed;
		inset: auto 0 0;
		width: 100%;
		max-width: none;
		max-height: calc(100dvh - 1rem);
		margin: 0;
		padding: 0 0 env(safe-area-inset-bottom);
		border-radius: 10px 10px 0 0;
		overflow-y: auto;
	}
	.dialog-heading {
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
	@media (max-width: 57.499rem) {
		.preview-panel {
			display: none;
		}
	}
</style>
