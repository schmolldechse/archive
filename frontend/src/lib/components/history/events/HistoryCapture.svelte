<script lang="ts">
	import type { SnapshotHistoryItemResponse } from "$api";
	import { historyData, historyNumber, historyString } from "../history-events";

	let { item }: { item: SnapshotHistoryItemResponse } = $props();
	const data = $derived(historyData(item));
	const quality = $derived(historyString(data, "quality"));
	const status = $derived(historyString(data, "status"));
	const bytes = $derived(historyNumber(data, "storageBytes"));
	const resources = $derived(historyNumber(data, "resourceCount"));
</script>

{#if quality || status || bytes !== null || resources !== null}
	<dl>
		{#if quality}<div>
				<dt>Quality</dt>
				<dd>{quality === "COMPLETE" ? "Complete" : "Incomplete"}</dd>
			</div>{/if}
		{#if status}<div>
				<dt>Status</dt>
				<dd>{status.toLowerCase().replaceAll("_", " ")}</dd>
			</div>{/if}
		{#if bytes !== null}<div>
				<dt>Stored size</dt>
				<dd>{bytes.toLocaleString()} bytes</dd>
			</div>{/if}
		{#if resources !== null}<div>
				<dt>Resources</dt>
				<dd>{resources.toLocaleString()}</dd>
			</div>{/if}
	</dl>
{/if}

<style>
	dl {
		display: flex;
		flex-wrap: wrap;
		gap: 0.75rem 1.5rem;
		margin: 0.75rem 0 0;
	}
	dl > div {
		display: grid;
		gap: 0.125rem;
	}
	dt {
		color: var(--marginal-note);
		font-size: 0.6875rem;
		font-weight: 600;
		letter-spacing: 0.04em;
		text-transform: uppercase;
	}
	dd {
		margin: 0;
		font: 500 0.8125rem/1.4 var(--font-record);
		text-transform: capitalize;
	}
</style>
