<script lang="ts">
	import type { SnapshotModerationStatus } from "$api";
	import Check from "@lucide/svelte/icons/check";
	import CircleDashed from "@lucide/svelte/icons/circle-dashed";
	import RotateCcw from "@lucide/svelte/icons/rotate-ccw";
	import X from "@lucide/svelte/icons/x";
	let { status }: { status: SnapshotModerationStatus } = $props();
	const labels = { PENDING: "Pending review", APPROVED: "Approved", REJECTED: "Rejected", RESUBMITTED: "Resubmitted" };
</script>

<span class="moderation-status" data-component="snapshot-moderation-status" data-status={status}>
	{#if status === "APPROVED"}<Check aria-hidden="true" />
	{:else if status === "REJECTED"}<X aria-hidden="true" />
	{:else if status === "RESUBMITTED"}<RotateCcw aria-hidden="true" />
	{:else}<CircleDashed aria-hidden="true" />{/if}
	{labels[status] ?? "Review status unavailable"}
</span>

<style>
	.moderation-status {
		display: inline-flex;
		align-items: center;
		gap: 0.375rem;
		font: 600 0.875rem/1.4 var(--font-interface);
		color: var(--warning-ochre);
	}
	.moderation-status :global(svg) {
		width: 1rem;
		height: 1rem;
		flex: none;
	}
	[data-status="APPROVED"] {
		color: var(--preservation-green);
	}
	[data-status="REJECTED"] {
		color: var(--time-marker);
	}
	[data-status="RESUBMITTED"] {
		color: var(--reading-ink);
	}
</style>
