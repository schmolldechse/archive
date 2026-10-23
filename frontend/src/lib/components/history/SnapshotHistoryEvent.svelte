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
	<p class="history-category">{presentation.category}</p>
	<div class="history-title-row">
		<h3>{presentation.title}</h3>
		<div class="history-event-time">
			<time class="history-timestamp" datetime={dateTimeValue}>{timestamp}</time>
		</div>
	</div>
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
	.history-title-row {
		min-width: 0;
	}
	.history-event-time {
		display: none;
	}
	.history-timestamp {
		color: var(--reading-ink);
		font-size: 0.875rem;
		font-weight: 500;
		line-height: 1.4;
		white-space: nowrap;
	}
	@container (max-width: 32rem) {
		/* The title owns the time on phones, keeping category and date from crowding one row. */
		.history-title-row {
			display: flex;
			align-items: baseline;
			justify-content: space-between;
			gap: 0.5rem;
			flex-wrap: wrap;
		}
		h3 {
			min-width: 0;
			flex: 1 1 auto;
		}
		.history-event-time {
			min-width: 0;
			display: block;
			max-width: 100%;
			flex: 0 1 auto;
		}
	}
	@container (max-width: 26rem) {
		.history-title-row {
			display: grid;
			grid-template-columns: minmax(0, 1fr);
			gap: 0.25rem;
		}
		.history-event-time {
			justify-self: start;
		}
		.history-timestamp {
			white-space: normal;
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
