<script lang="ts">
	import { navigating } from "$app/state";
	import { untrack } from "svelte";
	import ChevronRight from "@lucide/svelte/icons/chevron-right";
	import RotateCcw from "@lucide/svelte/icons/rotate-ccw";
	import Button from "$lib/components/ui/Button.svelte";
	import Separator from "$lib/components/ui/Separator.svelte";
	import { InputRoot, InputLabel, InputControl } from "$lib/components/ui/input";
	import {
		SelectRoot,
		SelectLabel,
		SelectTrigger,
		SelectValue,
		SelectContent,
		SelectViewport,
		SelectItem
	} from "$lib/components/ui/select";

	let { filters, review, resetHref }: { filters: { q: string; status: string }; review: boolean; resetHref: string } = $props();

	const statuses = $derived([
		{ value: "", label: review ? "All review requests" : "All statuses" },
		{ value: "PENDING", label: review ? "Pending first review" : "Pending review" },
		{ value: "RESUBMITTED", label: "Resubmitted" },
		{ value: "APPROVED", label: "Approved" },
		{ value: "REJECTED", label: "Rejected" }
	]);
	let query = $state(untrack(() => filters.q));
	let status = $state<string | null>(untrack(() => filters.status));
	const pending = $derived(navigating.to?.url.pathname === resetHref);
	const hasFilters = $derived(Boolean(query.trim() || status || filters.q || filters.status));

	function clearDraft(): void {
		query = "";
		status = "";
	}
</script>

<form
	method="GET"
	role="search"
	aria-label="Search snapshot submissions"
	aria-busy={pending ? "true" : undefined}
	class="submission-search"
>
	<div class="search-main">
		<InputRoot id="submission-search" disabled={pending}>
			<InputLabel>Search snapshots <span class="field-qualifier">Optional</span></InputLabel>
			<InputControl
				type="search"
				name="q"
				value={query}
				maxlength={500}
				placeholder="Title or source URL"
				autocomplete="off"
				spellcheck={false}
				oninput={(event) => (query = event.currentTarget.value)}
			/>
		</InputRoot>
		<Button type="submit" variant="primary" size="large" loading={pending} loadingLabel="Searching…">
			Search<ChevronRight aria-hidden="true" />
		</Button>
	</div>
	<Separator />
	<div class="search-filters">
		<SelectRoot type="single" name="moderationStatus" bind:value={status} disabled={pending} class="status-filter">
			<SelectLabel>Moderation status <span class="field-qualifier">Optional</span></SelectLabel>
			<SelectTrigger>
				<SelectValue>{statuses.find((item) => item.value === status)?.label ?? statuses[0].label}</SelectValue>
			</SelectTrigger>
			<SelectContent>
				<SelectViewport>
					{#each statuses as item (item.value)}
						<SelectItem value={item.value} textValue={item.label}>{item.label}</SelectItem>
					{/each}
				</SelectViewport>
			</SelectContent>
		</SelectRoot>
		<Button href={resetHref} variant="text" disabled={pending || !hasFilters} onclick={clearDraft}>
			<RotateCcw aria-hidden="true" />Reset filters
		</Button>
	</div>
</form>

<style>
	.submission-search {
		border-top: 2px solid var(--reading-ink);
		background: var(--archive-layer);
	}
	.submission-search :global([data-input-control]),
	.submission-search :global([data-select-trigger]) {
		background: var(--reading-room);
	}
	.search-main {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto;
		align-items: start;
		gap: 1rem;
		padding: 1.5rem;
	}
	.search-main > :global(button) {
		min-height: 56px;
		margin-top: 1.8125rem;
	}
	.search-filters {
		display: flex;
		flex-wrap: wrap;
		align-items: end;
		justify-content: space-between;
		gap: 1rem 2rem;
		padding: 1.25rem 1.5rem;
	}
	.search-filters :global(.status-filter) {
		max-width: 17rem;
	}
	@media (max-width: 42rem) {
		.search-main {
			grid-template-columns: minmax(0, 1fr);
			padding: 1rem;
		}
		.search-main > :global(button) {
			width: 100%;
			margin-top: 0;
		}
		.search-filters {
			padding: 1rem;
		}
		.search-filters :global(.status-filter) {
			max-width: none;
		}
		.search-filters > :global(a) {
			margin-inline-start: auto;
		}
	}
</style>
