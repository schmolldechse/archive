<script lang="ts">
	import ChevronRight from "@lucide/svelte/icons/chevron-right";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import X from "@lucide/svelte/icons/x";
	import Button from "$lib/components/ui/Button.svelte";
	import Separator from "$lib/components/ui/Separator.svelte";
	import { InputControl, InputError, InputLabel, InputRoot } from "$lib/components/ui/input";
	import SearchFilters from "./SearchFilters.svelte";
	import { searchHref, type SearchDraft, type SearchErrors, type SearchState } from "./search-state";
	let {
		draft,
		committed,
		errors,
		pending = false,
		onChange,
		onSubmit,
		onReset
	}: {
		draft: SearchDraft;
		committed: SearchState;
		errors: SearchErrors;
		pending?: boolean;
		onChange: (patch: Partial<SearchDraft>) => void;
		onSubmit: () => void;
		onReset: () => void;
	} = $props();
</script>

<section class="search-notation" id="browse" aria-labelledby="search-notation-title" data-component="search-notation">
	<div class="section-heading">
		<div>
			<p class="section-label">01 / Search notation</p>
			<h2 id="search-notation-title">Locate a preserved source</h2>
		</div>
	</div>
	<form
		role="search"
		onsubmit={(event) => {
			event.preventDefault();
			onSubmit();
		}}
		novalidate
	>
		<div class="query-section">
			<div class="search-main">
				<InputRoot id="snapshot-query" invalid={Boolean(errors.query)} class="query-field">
					<InputLabel>Exact snapshot link</InputLabel>
					<div class="query-input">
						<InputControl
							type="url"
							value={draft.query}
							autocomplete="off"
							autocapitalize="none"
							spellcheck={false}
							name="sourceUrl"
							data-value-kind="record"
							placeholder="https://example.org/complete/path"
							oninput={(event) => onChange({ query: event.currentTarget.value })}
						/>
						{#if draft.query}<Button
								variant="text"
								class="clear-query"
								aria-label="Clear exact snapshot link"
								onclick={() => onChange({ query: "" })}><X aria-hidden="true" /></Button
							>{/if}
					</div>
					{#if errors.query}<InputError>{errors.query}</InputError>{/if}
				</InputRoot>
				<Button type="submit" variant="primary" size="large" loading={pending} loadingLabel="Searching register…"
					>Search register<ChevronRight aria-hidden="true" /></Button
				>
			</div>
			{#if errors.form}<p role="alert" class="form-error"><CircleAlert aria-hidden="true" />{errors.form}</p>{/if}
		</div>
		<Separator />
		{#key searchHref({ ...committed, page: 1 })}
			<SearchFilters
				{draft}
				{errors}
				{onChange}
				{pending}
				{onReset}
				applied={Boolean(
					committed.title ||
					committed.tags.length ||
					committed.sourceType ||
					committed.quality ||
					committed.capturedFrom ||
					committed.capturedUntil ||
					committed.order === "asc"
				)}
			/>
		{/key}
	</form>
</section>

<style>
	.search-notation {
		padding-block: 3rem;
		border-bottom: 1px solid var(--border-trace);
	}
	form {
		border-top: 2px solid var(--reading-ink);
		background: var(--archive-layer);
	}
	form :global([data-input-control]),
	form :global([data-select-trigger]) {
		background: var(--reading-room);
	}
	.section-heading {
		display: flex;
		align-items: end;
		justify-content: space-between;
		gap: 2rem;
		margin-bottom: 1rem;
	}
	.section-label {
		margin: 0 0 0.5rem;
		font-size: 0.75rem;
		text-transform: uppercase;
		letter-spacing: 0.08em;
		color: var(--marginal-note);
	}
	h2 {
		margin: 0;
		font-size: 2rem;
		font-weight: 500;
		line-height: 1.18;
		letter-spacing: -0.015em;
	}
	.search-main {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto;
		align-items: start;
		gap: 1rem;
		padding: 1.5rem;
	}
	.search-main > :global(button) {
		margin-top: 1.8125rem;
		min-height: 56px;
	}
	.query-input {
		position: relative;
	}
	.query-input :global(input) {
		padding-right: 3.5rem;
	}
	.query-input :global(input::placeholder) {
		font-family: var(--font-interface);
	}
	:global(.clear-query) {
		position: absolute;
		right: 0.25rem;
		top: 0.375rem;
		min-width: 44px;
		min-height: 44px;
		padding: 0.5rem;
	}
	.form-error {
		display: flex;
		align-items: start;
		gap: 0.5rem;
		color: var(--time-marker);
		margin: 0;
		padding: 0 1.5rem 1rem;
	}
	.form-error :global(svg) {
		width: 1.25rem;
		flex: none;
	}
	@media (max-width: 42rem) {
		.section-heading {
			display: grid;
			gap: 1rem;
		}
		h2 {
			font-size: 1.75rem;
		}
		.search-main {
			grid-template-columns: 1fr;
		}
		.search-main > :global(button) {
			margin: 0;
			width: 100%;
		}
		.search-main {
			padding: 1rem;
		}
		.form-error {
			padding: 0 1rem 1rem;
		}
	}
</style>
