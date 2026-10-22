<script lang="ts">
	import type { SnapshotHistoryItemResponse } from "$api";
	import ArchiveAttribution from "$lib/components/archive/ArchiveAttribution.svelte";
	import { historyEventComponent, historyPresentation } from "./history-events";

	let { item }: { item: SnapshotHistoryItemResponse } = $props();
	const presentation = $derived(historyPresentation(item));
	const EventDetails = $derived(historyEventComponent(item));
</script>

<article class="history-event">
	<p class="history-category">{presentation.category}</p>
	<h3>{presentation.title}</h3>
	{#if item.actor}
		<div class="history-actor"><ArchiveAttribution creator={item.actor} layout="compact" label="Actor" /></div>
	{/if}
	<EventDetails {item} />
</article>

<style>
	.history-event {
		min-width: 0;
		padding-bottom: 1.5rem;
		border-bottom: 1px solid var(--border-trace);
	}
	.history-category {
		margin: 0 0 0.375rem;
		color: var(--history-tone, var(--marginal-note));
		font-size: 0.6875rem;
		font-weight: 700;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	h3 {
		margin: 0;
		font: 500 1.375rem/1.3 var(--font-editorial);
		letter-spacing: -0.005em;
	}
	.history-actor {
		margin-top: 0.75rem;
	}
	@media (max-width: 48rem) {
		.history-event {
			padding-bottom: 1.25rem;
		}
		h3 {
			font-size: 1.25rem;
		}
	}
</style>
