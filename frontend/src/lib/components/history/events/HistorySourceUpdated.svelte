<script lang="ts">
	import type { SnapshotHistoryItemResponse } from "$api";
	import { historyBoolean, historyData, historyString } from "../history-events";

	let { item }: { item: SnapshotHistoryItemResponse } = $props();
	const data = $derived(historyData(item));
	const previousSource = $derived(historyString(data, "previousSourceUrl"));
	const source = $derived(historyString(data, "sourceUrl"));
	const reviewRequired = $derived(historyBoolean(data, "reviewRequired"));
</script>

<dl>
	{#if previousSource}<div>
			<dt>Previous source</dt>
			<dd>{previousSource}</dd>
		</div>{/if}
	{#if source}<div>
			<dt>New source</dt>
			<dd>{source}</dd>
		</div>{/if}
</dl>
{#if reviewRequired}<p class="review-note">A new review was requested because the source provenance changed.</p>{/if}

<style>
	dl {
		display: grid;
		gap: 0.5rem;
		margin: 0.75rem 0 0;
	}
	dl > div {
		display: grid;
		gap: 0.125rem;
	}
	dt {
		color: var(--marginal-note);
		font-size: 0.75rem;
	}
	dd {
		margin: 0;
		font: 400 0.8125rem/1.5 var(--font-record);
		overflow-wrap: anywhere;
		word-break: break-word;
	}
	.review-note {
		margin: 0.75rem 0 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
</style>
