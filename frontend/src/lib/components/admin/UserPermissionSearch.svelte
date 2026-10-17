<script lang="ts">
	import { navigating } from "$app/state";
	import ChevronRight from "@lucide/svelte/icons/chevron-right";
	import Button from "$lib/components/ui/Button.svelte";
	import { InputRoot, InputLabel, InputControl } from "$lib/components/ui/input";

	let { query, context, resetHref }: { query: string; context: URLSearchParams; resetHref: string } = $props();
	const pending = $derived(Boolean(navigating.to?.url.pathname.startsWith("/admin/users/@")));
</script>

<!-- Preserve the parent user-list filters without submitting the current permission page. -->
<form
	method="GET"
	role="search"
	aria-label="Search permissions"
	aria-busy={pending ? "true" : undefined}
	class="permission-search"
>
	{#each [...context] as [key, value]}<input type="hidden" name={key} {value} />{/each}
	<InputRoot id="admin-permission-query" disabled={pending}>
		<InputLabel>Search permissions <span class="field-qualifier">Optional</span></InputLabel>
		<InputControl
			type="search"
			name="permissionQ"
			value={query}
			maxlength={128}
			placeholder="Name or permission key"
			autocomplete="off"
		/>
	</InputRoot>
	<div class="search-actions">
		<Button type="submit" size="large" loading={pending} loadingLabel="Searching…"
			>Search<ChevronRight aria-hidden="true" /></Button
		>
		{#if query}<Button href={resetHref} variant="text" disabled={pending}>Clear</Button>{/if}
	</div>
</form>

<style>
	.permission-search {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto;
		align-items: start;
		gap: 1rem;
		margin-block: 1.25rem 1.5rem;
	}
	.permission-search :global([data-input-control]) {
		background: var(--reading-room);
		scroll-margin-block: 8rem;
	}
	.search-actions {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 0.75rem;
		margin-top: 1.8125rem;
	}
	.search-actions > :global(button) {
		min-height: 56px;
	}
	@media (max-width: 42rem) {
		.permission-search {
			grid-template-columns: minmax(0, 1fr);
		}
		.search-actions {
			margin-top: 0;
		}
		.search-actions > :global(button) {
			flex: 1;
		}
	}
</style>
