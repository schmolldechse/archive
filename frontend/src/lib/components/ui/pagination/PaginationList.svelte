<script lang="ts">
	import { getPaginationContext } from "./pagination-context.svelte";
	import type { PaginationListProps } from "./types";

	let { children, ref = $bindable(null), class: className, ...restProps }: PaginationListProps = $props();

	getPaginationContext("PaginationList");
</script>

<ul {...restProps} bind:this={ref} class={["pagination-list", className]} data-pagination-list>
	{@render children()}
</ul>

<style>
	.pagination-list {
		display: flex;
		max-width: 100%;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.25rem;
		margin: 0;
		padding: 0;
		list-style: none;
	}
</style>
