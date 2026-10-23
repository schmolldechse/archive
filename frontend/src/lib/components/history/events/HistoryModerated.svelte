<script lang="ts">
	import type { SnapshotHistoryItemResponse } from "$api";
	import { historyData, historyString } from "../history-events";

	let { item }: { item: SnapshotHistoryItemResponse } = $props();
	const data = $derived(historyData(item));
	const reason = $derived(historyString(data, "reason"));
</script>

{#if reason}
	<div class="event-note">
		<p class="event-note__label">Review note</p>
		<p>{reason}</p>
	</div>
{/if}

<style>
	.event-note {
		max-width: 72ch;
		margin-top: 1rem;
		border-inline-start: 2px solid var(--border-trace);
		padding: 0.625rem 0 0.625rem 1rem;
	}
	.event-note__label {
		margin: 0 0 0.25rem;
		color: var(--marginal-note);
		font-size: 0.6875rem;
		font-weight: 700;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	.event-note p:last-child {
		margin: 0;
		font-size: 0.9375rem;
		line-height: 1.55;
		white-space: pre-wrap;
		overflow-wrap: anywhere;
	}
</style>
