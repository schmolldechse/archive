<script lang="ts">
	import type { SnapshotHistoryItemResponse } from "$api";
	import { historyBoolean, historyData } from "../history-events";

	let { item }: { item: SnapshotHistoryItemResponse } = $props();
	const data = $derived(historyData(item));
	const wasPrivate = $derived(historyBoolean(data, "previousIsPrivate"));
	const isPrivate = $derived(historyBoolean(data, "isPrivate"));
</script>

<dl>
	<div>
		<dt>Previous visibility</dt>
		<dd>{wasPrivate ? "Private" : "Public"}</dd>
	</div>
	<div>
		<dt>New visibility</dt>
		<dd>{isPrivate ? "Private" : "Public"}</dd>
	</div>
</dl>

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
	}
</style>
