<script lang="ts">
	import X from "@lucide/svelte/icons/x";
	import Button from "$lib/components/ui/Button.svelte";
	import type { FilterChip, SearchDraft } from "./search-state";
	let {
		chips,
		disabled = false,
		onRemove,
		onClear
	}: { chips: FilterChip[]; disabled?: boolean; onRemove: (field: keyof SearchDraft) => void; onClear: () => void } = $props();
</script>

{#if chips.length}
	<div class="applied-filters" aria-label="Applied search filters" data-component="applied-filters">
		<span class="label">Applied</span>
		{#each chips as chip (chip.field)}
			<Button
				{disabled}
				class="filter-chip"
				aria-label={`Remove ${chip.label} and submit the search form`}
				onclick={() => onRemove(chip.field)}>{chip.label}<X aria-hidden="true" /></Button
			>
		{/each}
		<Button variant="text" {disabled} onclick={onClear}>Clear all</Button>
	</div>
{/if}

<style>
	.applied-filters {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.5rem;
		padding: 1rem 1.5rem;
		border-top: 1px solid var(--border-trace);
	}
	.label {
		font-size: 0.75rem;
		text-transform: uppercase;
		letter-spacing: 0.08em;
		color: var(--marginal-note);
		margin-right: 0.5rem;
	}
	:global(.filter-chip) {
		max-width: 100%;
		font-size: 0.875rem;
		text-align: left;
		overflow-wrap: anywhere;
	}
	:global(.filter-chip svg) {
		flex: none;
	}
	@media (max-width: 42rem) {
		.applied-filters {
			padding: 1rem;
		}
	}
</style>
