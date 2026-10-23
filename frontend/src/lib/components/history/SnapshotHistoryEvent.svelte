<script lang="ts">
	import type { SnapshotHistoryItemResponse } from "$api";
	import ArchiveAttribution from "$lib/components/archive/ArchiveAttribution.svelte";
	import { historyEventComponent, historyPresentation } from "./history-events";

	let {
		item,
		timestamp,
		dateTimeValue
	}: { item: SnapshotHistoryItemResponse; timestamp: string; dateTimeValue: string } = $props();
	const presentation = $derived(historyPresentation(item));
	const EventDetails = $derived(historyEventComponent(item));
</script>

<article class="history-event">
	<div class="history-event-kicker">
		<p class="history-category">{presentation.category}</p>
		<div class="history-event-time">
			<time class="history-timestamp" datetime={dateTimeValue}>{timestamp}</time>
		</div>
	</div>
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
	.history-event-kicker {
		display: flex;
		align-items: center;
		min-height: 1.25rem;
	}
	.history-category {
		margin: 0;
		color: var(--history-tone, var(--marginal-note));
		font-size: 0.6875rem;
		font-weight: 700;
		letter-spacing: 0.08em;
		line-height: 1.25rem;
		text-transform: uppercase;
	}
	.history-event-time {
		display: none;
		line-height: 1.25rem;
	}
	.history-timestamp {
		color: var(--reading-ink);
		font-size: 0.875rem;
		font-weight: 500;
		line-height: 1.25rem;
		white-space: nowrap;
	}
	@container (max-width: 32rem) {
		.history-event-kicker {
			justify-content: space-between;
			gap: 0.5rem;
		}
		.history-event-time {
			display: block;
			flex: 0 0 auto;
		}
		.history-timestamp {
			font-size: 0.75rem;
		}
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
