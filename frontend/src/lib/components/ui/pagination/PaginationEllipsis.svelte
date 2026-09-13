<script lang="ts">
	import { getPaginationContext } from "./pagination-context.svelte";
	import type { PaginationEllipsisProps } from "./types";

	let { label = "More pages", ref = $bindable(null), class: className, ...restProps }: PaginationEllipsisProps = $props();

	getPaginationContext("PaginationEllipsis");
</script>

<li {...restProps} bind:this={ref} class={["pagination-ellipsis", className]} aria-hidden="true" data-pagination-ellipsis>
	<span class="pagination-ellipsis__mark">…</span>
	{#if label}<span class="pagination-ellipsis__label">{label}</span>{/if}
</li>

<style>
	.pagination-ellipsis {
		display: inline-flex;
		min-width: 1.5rem;
		min-height: 2.75rem;
		align-items: center;
		justify-content: center;
		color: var(--marginal-note);
		font-variant-numeric: tabular-nums;
		user-select: none;
	}

	.pagination-ellipsis__mark {
		line-height: 1;
	}

	.pagination-ellipsis__label {
		position: absolute;
		width: 1px;
		height: 1px;
		margin: -1px;
		padding: 0;
		border: 0;
		overflow: hidden;
		clip: rect(0 0 0 0);
		clip-path: inset(50%);
		white-space: nowrap;
	}
</style>
