<script lang="ts">
	import Sliders from "@lucide/svelte/icons/sliders-horizontal";
	import { SnapshotQuality, SourceType } from "$api";
	import Button from "$lib/components/ui/Button.svelte";
	import { InputControl, InputError, InputLabel, InputRoot } from "$lib/components/ui/input";
	import CaptureDateFields from "./CaptureDateFields.svelte";
	import SearchSelect from "./SearchSelect.svelte";
	import type { SearchDraft, SearchErrors } from "./search-state";
	let {
		draft,
		errors,
		applied = false,
		onChange
	}: {
		draft: SearchDraft;
		errors: SearchErrors;
		applied?: boolean;
		onChange: (patch: Partial<SearchDraft>) => void;
	} = $props();
	let expanded = $state<boolean | null>(null);
	const fields = [
		{ key: "title", label: "Title contains", placeholder: "e.g. observatory" },
		{ key: "tags", label: "Required tags", placeholder: "data, science" }
	] as const;
	const count = $derived(
		[draft.title, draft.tags, draft.sourceType, draft.quality, draft.from, draft.through].filter((value) => value.trim())
			.length + (draft.order === "asc" ? 1 : 0)
	);
	const forcedOpen = $derived(
		fields.some((field) => errors[field.key]) ||
			Boolean(errors.from || errors.through || errors.sourceType || errors.quality || errors.order)
	);
	const open = $derived((expanded ?? applied) || forcedOpen);
	function change(patch: Partial<SearchDraft>): void {
		expanded = true;
		onChange(patch);
	}
</script>

<div class="search-filters" data-component="search-filters">
	<Button
		aria-expanded={open}
		aria-controls="query-notation"
		onclick={() => {
			expanded = !open;
		}}
	>
		<Sliders aria-hidden="true" /> Query notation {#if count}<span class="filter-count">{count}</span>{/if}
	</Button>
	<div id="query-notation" hidden={!open} class="advanced-filters">
		<div class="filter-grid">
			{#each fields as field (field.key)}
				<InputRoot id={`search-${field.key}`} invalid={Boolean(errors[field.key])} class="wide-field">
					<InputLabel>{field.label}</InputLabel>
					<InputControl
						name={field.key}
						value={draft[field.key]}
						placeholder={field.placeholder}
						oninput={(event) => change({ [field.key]: event.currentTarget.value })}
					/>
					{#if errors[field.key]}<InputError>{errors[field.key]}</InputError>{/if}
				</InputRoot>
			{/each}
			<SearchSelect
				label="Source type"
				value={draft.sourceType}
				error={errors.sourceType}
				options={[
					{ value: "", label: "Any source" },
					{ value: SourceType.URL, label: "Web URL" },
					{ value: SourceType.HTML, label: "Uploaded HTML" }
				]}
				onChange={(value) => change({ sourceType: (value ?? "") as SearchDraft["sourceType"] })}
			/>
			<SearchSelect
				label="Capture quality"
				value={draft.quality}
				error={errors.quality}
				options={[
					{ value: "", label: "Any condition" },
					{ value: SnapshotQuality.COMPLETE, label: "Complete" },
					{ value: SnapshotQuality.INCOMPLETE, label: "Incomplete" }
				]}
				onChange={(value) => change({ quality: (value ?? "") as SearchDraft["quality"] })}
			/>
			<CaptureDateFields {draft} {errors} onChange={change} />
			<SearchSelect
				label="Capture order"
				value={errors.order ? null : draft.order}
				placeholder="Choose capture order"
				error={errors.order}
				options={[
					{ value: "desc", label: "Newest first" },
					{ value: "asc", label: "Oldest first" }
				]}
				onChange={(value) => {
					if (value === "asc" || value === "desc") change({ order: value });
				}}
			/>
		</div>
	</div>
</div>

<style>
	.search-filters {
		border-top: 1px solid var(--border-trace);
		padding: 1rem 1.5rem;
	}
	.advanced-filters {
		margin-top: 1.5rem;
		border-top: 1px solid var(--border-trace);
		padding-top: 1.5rem;
	}
	.advanced-filters[hidden] {
		display: none;
	}
	.filter-grid {
		display: grid;
		grid-template-columns: repeat(4, minmax(0, 1fr));
		gap: 1.5rem 1rem;
	}
	.filter-grid :global([data-input-root]),
	.filter-grid :global([data-select-root]) {
		min-width: 0;
	}
	.filter-grid :global(.wide-field) {
		grid-column: span 2;
	}
	.filter-count {
		font-family: var(--font-record);
		font-size: 0.75rem;
	}
	@media (max-width: 72rem) {
		.filter-grid {
			grid-template-columns: repeat(3, minmax(0, 1fr));
		}
	}
	@media (max-width: 57.499rem) {
		.filter-grid {
			grid-template-columns: repeat(2, minmax(0, 1fr));
		}
	}
	@media (max-width: 42rem) {
		.search-filters {
			padding: 1rem;
		}
		.filter-grid {
			grid-template-columns: 1fr;
		}
		.filter-grid :global(.wide-field) {
			grid-column: auto;
		}
	}
</style>
