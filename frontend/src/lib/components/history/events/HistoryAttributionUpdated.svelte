<script lang="ts">
	import type { SnapshotHistoryItemResponse } from "$api";
	import ArchiveAttribution from "$lib/components/archive/ArchiveAttribution.svelte";
	import { historyBoolean, historyData, historyUser } from "../history-events";

	let { item }: { item: SnapshotHistoryItemResponse } = $props();
	const data = $derived(historyData(item));
	const previousUser = $derived(historyUser(data, "previousUser"));
	const user = $derived(historyUser(data, "user"));
	const removed = $derived(historyBoolean(data, "removed"));
	const redacted = $derived(historyBoolean(data, "redacted"));
</script>

<div class="attribution-change">
	{#if previousUser}
		<div><span>Previous</span><ArchiveAttribution creator={previousUser} layout="compact" label="Previous owner" /></div>
	{/if}
	<div>
		<span>Current</span>
		{#if user}
			<ArchiveAttribution creator={user} layout="compact" label="Current owner" />
		{:else if removed}
			<p>Account attribution removed</p>
		{:else if redacted}
			<p>Earlier attribution target withheld</p>
		{:else}
			<p>Account attribution updated</p>
		{/if}
	</div>
</div>

<style>
	.attribution-change {
		display: grid;
		gap: 0.75rem;
		margin-top: 0.75rem;
	}
	.attribution-change > div {
		display: grid;
		gap: 0.25rem;
	}
	.attribution-change span {
		color: var(--marginal-note);
		font-size: 0.6875rem;
		font-weight: 700;
		letter-spacing: 0.06em;
		text-transform: uppercase;
	}
	.attribution-change p {
		margin: 0;
		font-size: 0.875rem;
	}
</style>
