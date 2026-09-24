<script lang="ts">
	import Sliders from "@lucide/svelte/icons/sliders-horizontal";
	import { SnapshotQuality, SourceType } from "$api";
	import Badge from "$lib/components/ui/Badge.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import {
		AccordionContent,
		AccordionHeader,
		AccordionItem,
		AccordionRoot,
		AccordionTrigger
	} from "$lib/components/ui/accordion";
	import { InputControl, InputDescription, InputError, InputLabel, InputRoot } from "$lib/components/ui/input";
	import CaptureDateField from "./CaptureDateField.svelte";
	import SearchSelect from "./SearchSelect.svelte";
	import TagEditor from "$lib/components/tags/TagEditor.svelte";
	import type { SearchDraft, SearchErrors } from "./search-state";

	let {
		draft,
		errors,
		applied = false,
		pending = false,
		onChange,
		onReset
	}: {
		draft: SearchDraft;
		errors: SearchErrors;
		applied?: boolean;
		pending?: boolean;
		onChange: (patch: Partial<SearchDraft>) => void;
		onReset: () => void;
	} = $props();

	let expanded = $state<boolean | null>(null);
	const count = $derived(
		draft.tags.length +
			[draft.title, draft.sourceType, draft.quality, draft.from, draft.through].filter((value) => value.trim()).length +
			(draft.order === "asc" ? 1 : 0)
	);
	const forcedOpen = $derived(
		Boolean(errors.title || errors.tags || errors.from || errors.through || errors.sourceType || errors.quality || errors.order)
	);
	const open = $derived((expanded ?? applied) || forcedOpen);
	const filterCountLabel = $derived(`${count} active filter${count === 1 ? "" : "s"}`);

	function change(patch: Partial<SearchDraft>): void {
		expanded = true;
		onChange(patch);
	}
</script>

<section class="search-filters" aria-labelledby="search-filters-heading" data-component="search-filters">
	<AccordionRoot
		type="single"
		value={open ? "filters" : null}
		onValueChange={(value) => (expanded = value === "filters")}
		headingLevel={3}
		class="filter-accordion"
	>
		<AccordionItem value="filters">
			<div class="filter-toolbar">
				<AccordionHeader id="search-filters-heading">
					<AccordionTrigger class="filter-trigger">
						<span class="filter-trigger__content">
							<Sliders aria-hidden="true" />
							<span class="filter-trigger__label">Filters</span>
							<Badge variant={count > 0 ? "info" : "neutral"} size="compact" class="filter-count">{filterCountLabel}</Badge>
						</span>
					</AccordionTrigger>
				</AccordionHeader>
				<Button class="filter-reset" variant="text" disabled={pending || count === 0} onclick={onReset}>Reset filters</Button>
			</div>
			<AccordionContent region class="filter-content">
				<div class="filter-grid">
					<InputRoot id="search-title" invalid={Boolean(errors.title)} hasDescription class="wide-field">
						<InputLabel>Title contains <span class="field-qualifier">Optional</span></InputLabel>
						<InputControl
							name="title"
							value={draft.title}
							placeholder="e.g. observatory"
							oninput={(event) => change({ title: event.currentTarget.value })}
						/>
						<InputDescription>Find records with these words in the title.</InputDescription>
						{#if errors.title}<InputError>{errors.title}</InputError>{/if}
					</InputRoot>
					<TagEditor
						id="search-tags"
						label="Tags to match"
						requirement="optional"
						listLabel="Added required tags"
						description="Add a tag and press Enter. Results must include every tag you add."
						tags={draft.tags}
						error={errors.tags}
						onChange={(tags) => change({ tags })}
					/>
					<SearchSelect
						label="Source type"
						description="Narrow results to a source format."
						value={draft.sourceType}
						error={errors.sourceType}
						options={[
							{ value: "", label: "Any source" },
							{ value: SourceType.URL, label: "Web URL" },
							{ value: SourceType.HTML, label: "Uploaded HTML" },
							{ value: SourceType.MHTML, label: "Uploaded MHTML" },
							{ value: SourceType.WEBARCHIVE, label: "Uploaded Webarchive" }
						]}
						onChange={(value) => change({ sourceType: (value ?? "") as SearchDraft["sourceType"] })}
					/>
					<SearchSelect
						label="Capture quality"
						description="Show captures by their recorded condition."
						value={draft.quality}
						error={errors.quality}
						options={[
							{ value: "", label: "Any condition" },
							{ value: SnapshotQuality.COMPLETE, label: "Complete" },
							{ value: SnapshotQuality.INCOMPLETE, label: "Incomplete" }
						]}
						onChange={(value) => change({ quality: (value ?? "") as SearchDraft["quality"] })}
					/>
					<SearchSelect
						class="order-field"
						label="Capture order"
						requirement="required"
						description="Choose which captures appear first."
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
					<CaptureDateField
						id="capture-from"
						label="Captured from"
						description="Include captures on or after this date."
						action="Choose start date"
						value={draft.from}
						error={errors.from}
						maxValue={draft.through}
						onChange={(value) => change({ from: value })}
					/>
					<CaptureDateField
						id="capture-through"
						label="Captured through"
						description="Include captures on or before this date."
						action="Choose end date"
						value={draft.through}
						error={errors.through}
						minValue={draft.from}
						onChange={(value) => change({ through: value })}
					/>
				</div>
			</AccordionContent>
		</AccordionItem>
	</AccordionRoot>
</section>

<style>
	.search-filters {
		padding: 0 1.5rem;
	}

	.search-filters :global([data-accordion-item]) {
		border: 0;
	}

	.filter-toolbar {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto;
		align-items: center;
		gap: 1rem;
		min-height: 4.75rem;
	}

	.filter-toolbar :global([data-accordion-trigger].filter-trigger) {
		width: auto;
		padding-inline: 0.25rem;
	}

	.filter-toolbar :global([data-accordion-header]) {
		width: fit-content;
		max-width: 100%;
	}

	.filter-trigger__content {
		display: flex;
		min-width: 0;
		align-items: center;
		gap: 0.625rem;
	}

	.filter-trigger__content > :global(svg) {
		width: 1.25rem;
		height: 1.25rem;
		flex: none;
		stroke-width: 1.75;
	}

	.filter-trigger__label,
	:global(.filter-count) {
		white-space: nowrap;
	}

	:global(.filter-count) {
		flex: none;
	}

	:global([data-accordion-content].filter-content) {
		border-top: 1px solid var(--border-trace);
	}

	:global([data-accordion-content].filter-content .accordion-content__inner) {
		padding: 1.5rem 0;
	}

	.filter-grid {
		display: grid;
		grid-template-columns: repeat(6, minmax(0, 1fr));
		align-items: start;
		gap: 1.5rem 1rem;
	}

	.filter-grid :global([data-input-root]),
	.filter-grid :global([data-select-root]),
	.filter-grid :global([data-component="capture-date-field"]) {
		min-width: 0;
	}

	.filter-grid :global(.wide-field),
	.filter-grid :global(.tag-editor) {
		grid-column: span 3;
	}

	.filter-grid :global([data-select-root]) {
		grid-column: span 2;
	}

	.filter-grid :global([data-component="capture-date-field"]) {
		grid-column: span 3;
	}

	@media (max-width: 57.499rem) {
		.filter-grid {
			grid-template-columns: repeat(2, minmax(0, 1fr));
		}

		.filter-grid :global(.wide-field),
		.filter-grid :global(.tag-editor),
		.filter-grid :global([data-select-root]),
		.filter-grid :global([data-component="capture-date-field"]) {
			grid-column: auto;
		}

		.filter-grid :global(.order-field) {
			grid-column: 1 / -1;
		}
	}

	@media (max-width: 42rem) {
		.search-filters {
			padding-inline: 1rem;
		}

		.filter-toolbar {
			grid-template-columns: minmax(0, 1fr);
			align-items: stretch;
			gap: 0.25rem;
			padding-block: 0.5rem;
		}

		.filter-toolbar :global([data-accordion-header]),
		.filter-toolbar :global([data-accordion-trigger].filter-trigger) {
			width: 100%;
		}

		.filter-toolbar :global(.filter-reset) {
			justify-self: end;
		}

		.filter-grid {
			grid-template-columns: 1fr;
		}

		.filter-grid :global(.wide-field),
		.filter-grid :global(.tag-editor),
		.filter-grid :global(.order-field) {
			grid-column: auto;
		}
	}
</style>
