<script lang="ts">
	import { untrack } from "svelte";
	import { navigating } from "$app/state";
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

	let { filters }: { filters: { q: string; role: string } } = $props();
	const roles = [
		{ value: "", label: "All roles" },
		{ value: "USER", label: "User" },
		{ value: "ADMIN", label: "Admin" }
	];
	// The page keys this draft by URL; search and pagination remain owned by the server.
	let query = $state(untrack(() => filters.q));
	let role = $state<string | null>(untrack(() => filters.role));
	const pending = $derived(navigating.to?.url.pathname === "/admin/users");
	const hasFilters = $derived(Boolean(query.trim() || role || filters.q || filters.role));

	function clearDraft(): void {
		query = "";
		role = "";
	}
</script>

<form method="GET" role="search" aria-label="Search users" aria-busy={pending ? "true" : undefined} class="user-search">
	<div class="search-main">
		<InputRoot id="admin-user-query" disabled={pending}>
			<InputLabel>Search users</InputLabel>
			<InputControl
				type="search"
				name="q"
				value={query}
				maxlength={128}
				placeholder="Handle, display name or role"
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
		<SelectRoot type="single" name="role" bind:value={role} disabled={pending} class="user-role-filter">
			<SelectLabel>Role</SelectLabel>
			<SelectTrigger><SelectValue>{roles.find((item) => item.value === role)?.label ?? "All roles"}</SelectValue></SelectTrigger
			>
			<SelectContent>
				<SelectViewport>
					{#each roles as item (item.value)}<SelectItem value={item.value} textValue={item.label}>{item.label}</SelectItem
						>{/each}
				</SelectViewport>
			</SelectContent>
		</SelectRoot>
		<Button href="/admin/users" variant="text" disabled={pending || !hasFilters} onclick={clearDraft}>
			<RotateCcw aria-hidden="true" />Reset filters
		</Button>
	</div>
</form>

<style>
	.user-search {
		border-top: 2px solid var(--reading-ink);
		background: var(--archive-layer);
	}
	.user-search :global([data-input-control]),
	.user-search :global([data-select-trigger]) {
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
		margin-top: 1.8125rem;
		min-height: 56px;
	}
	.search-filters {
		display: flex;
		flex-wrap: wrap;
		align-items: end;
		justify-content: space-between;
		gap: 1rem 2rem;
		padding: 1.25rem 1.5rem;
	}
	.search-filters :global(.user-role-filter) {
		max-width: 16rem;
	}
	@media (max-width: 42rem) {
		.search-main {
			grid-template-columns: minmax(0, 1fr);
			padding: 1rem;
		}
		.search-main > :global(button) {
			margin-top: 0;
			width: 100%;
		}
		.search-filters {
			padding: 1rem;
		}
		.search-filters :global(.user-role-filter) {
			max-width: none;
		}
		.search-filters > :global(a) {
			margin-inline-start: auto;
		}
	}
</style>
