<script lang="ts">
	import type { SnapshotHistoryItemResponse } from "$api";
	import { historyData, historyObject, historyStrings } from "../history-events";

	let { item }: { item: SnapshotHistoryItemResponse } = $props();
	const data = $derived(historyData(item));
	const titleChange = $derived(historyObject(data, "title"));
	const descriptionChange = $derived(historyObject(data, "description"));
	const tagsChange = $derived(historyObject(data, "tags"));
	const previousTags = $derived(tagsChange ? tagValues(tagsChange.previousValues) : null);
	const updatedTags = $derived(tagsChange ? tagValues(tagsChange.updatedValues) : null);
	const hasValueChanges = $derived(Boolean(titleChange || descriptionChange || tagsChange));
	const fields = $derived(historyStrings(data, "fields"));
	const addedTags = $derived(historyStrings(data, "addedTags"));
	const removedTags = $derived(historyStrings(data, "removedTags"));
	const fieldLabels: Record<string, string> = { TITLE: "Title", DESCRIPTION: "Description", TAGS: "Tags" };

	function textValue(value: unknown): string {
		if (value === null) return "Not set";
		if (typeof value !== "string") return "Not recorded";
		return value.length > 0 ? value : "(empty)";
	}

	function tagValues(value: unknown): string[] | null {
		if (!Array.isArray(value)) return null;
		const tags = value.filter((tag): tag is string => typeof tag === "string");
		return tags.length === value.length ? tags : null;
	}
</script>

{#if hasValueChanges}
	<dl class="field-changes" aria-label="Changed values">
		{#if titleChange}
			<div>
				<dt>Title</dt>
				<dd>
					<div>
						<span>Before</span>
						<p>{textValue(titleChange.previousValue)}</p>
					</div>
					<div>
						<span>After</span>
						<p>{textValue(titleChange.updatedValue)}</p>
					</div>
				</dd>
			</div>
		{/if}
		{#if descriptionChange}
			<div>
				<dt>Description</dt>
				<dd>
					<div>
						<span>Before</span>
						<p>{textValue(descriptionChange.previousValue)}</p>
					</div>
					<div>
						<span>After</span>
						<p>{textValue(descriptionChange.updatedValue)}</p>
					</div>
				</dd>
			</div>
		{/if}
		{#if tagsChange}
			<div>
				<dt>Tags</dt>
				<dd>
					<div>
						<span>Before</span>
						{#if previousTags === null}
							<p>Not recorded</p>
						{:else if previousTags.length}
							<ul class="tag-values">
								{#each previousTags as tag}<li>{tag}</li>{/each}
							</ul>
						{:else}
							<p>No tags</p>
						{/if}
					</div>
					<div>
						<span>After</span>
						{#if updatedTags === null}
							<p>Not recorded</p>
						{:else if updatedTags.length}
							<ul class="tag-values">
								{#each updatedTags as tag}<li>{tag}</li>{/each}
							</ul>
						{:else}
							<p>No tags</p>
						{/if}
					</div>
				</dd>
			</div>
		{/if}
	</dl>
{:else}
	{#if fields.length}
		<p class="changed-fields"><span>Changed</span>{fields.map((field) => fieldLabels[field] ?? field).join(", ")}</p>
	{/if}
	{#if addedTags.length || removedTags.length}
		<div class="tag-changes">
			{#if addedTags.length}<p><span>Added</span>{addedTags.join(", ")}</p>{/if}
			{#if removedTags.length}<p><span>Removed</span>{removedTags.join(", ")}</p>{/if}
		</div>
	{/if}
{/if}

<style>
	.field-changes {
		display: grid;
		gap: 0.75rem;
		margin: 0.75rem 0 0;
	}
	.field-changes > div {
		border-inline-start: 2px solid var(--border-trace);
		padding-inline-start: 1rem;
	}
	.field-changes dt {
		font-size: 0.875rem;
		font-weight: 600;
	}
	.field-changes dd {
		display: grid;
		gap: 0.375rem;
		margin: 0.375rem 0 0;
	}
	.field-changes dd > div {
		display: grid;
		grid-template-columns: 4rem minmax(0, 1fr);
		gap: 0.5rem;
	}
	.field-changes dd span {
		color: var(--marginal-note);
		font-size: 0.75rem;
	}
	.field-changes dd p {
		margin: 0;
		font-size: 0.875rem;
		line-height: 1.5;
		overflow-wrap: anywhere;
		white-space: pre-wrap;
	}
	.tag-values {
		display: flex;
		flex-wrap: wrap;
		gap: 0.375rem;
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.tag-values li {
		border: 1px solid var(--border-trace);
		border-radius: 999px;
		padding: 0.125rem 0.5rem;
		font-size: 0.8125rem;
		line-height: 1.4;
		overflow-wrap: anywhere;
	}
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
