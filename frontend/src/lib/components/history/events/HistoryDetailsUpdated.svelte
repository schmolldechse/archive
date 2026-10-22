<script lang="ts">
	import type { SnapshotHistoryItemResponse } from "$api";
	import { historyData, historyStrings } from "../history-events";

	let { item }: { item: SnapshotHistoryItemResponse } = $props();
	const data = $derived(historyData(item));
	const fields = $derived(historyStrings(data, "fields"));
	const addedTags = $derived(historyStrings(data, "addedTags"));
	const removedTags = $derived(historyStrings(data, "removedTags"));
	const fieldLabels: Record<string, string> = { TITLE: "Title", DESCRIPTION: "Description", TAGS: "Tags" };
</script>

{#if fields.length}
	<p class="changed-fields"><span>Changed</span>{fields.map((field) => fieldLabels[field] ?? field).join(", ")}</p>
{/if}
{#if addedTags.length || removedTags.length}
	<div class="tag-changes">
		{#if addedTags.length}<p><span>Added</span>{addedTags.join(", ")}</p>{/if}
		{#if removedTags.length}<p><span>Removed</span>{removedTags.join(", ")}</p>{/if}
	</div>
{/if}

<style>
	.changed-fields,
	.tag-changes p {
		margin: 0.625rem 0 0;
		font-size: 0.875rem;
		overflow-wrap: anywhere;
	}
	.changed-fields span,
	.tag-changes span {
		display: inline-block;
		min-width: 4.75rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.04em;
		text-transform: uppercase;
	}
	.tag-changes {
		margin-top: 0.75rem;
		border-inline-start: 2px solid var(--border-trace);
		padding-inline-start: 1rem;
		font-family: var(--font-record);
	}
</style>
