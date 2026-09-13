<script lang="ts">
	import ChevronRight from "@lucide/svelte/icons/chevron-right";

	import Button from "../Button.svelte";
	import { getPaginationContext } from "./pagination-context.svelte";
	import type { PaginationNextProps } from "./types";

	let {
		label,
		"aria-label": ariaLabel = "Next page",
		ref = $bindable(null),
		class: className,
		...restProps
	}: PaginationNextProps = $props();

	const pagination = getPaginationContext("PaginationNext");
	const destination = $derived(Math.min(pagination.page + 1, Math.max(pagination.pageCount, 1)));
	const unavailable = $derived(
		pagination.isPageUnavailable(destination) || pagination.pageCount === 0 || pagination.page >= pagination.pageCount
	);
	const handleClick = (event: MouseEvent): void => pagination.select(destination, event);
</script>

<li
	{...restProps}
	bind:this={ref}
	class={["pagination-direction", className]}
	data-pagination-next
	data-disabled={unavailable ? "" : undefined}
>
	{#if pagination.mode === "link"}
		<Button
			href={pagination.getHref(destination)}
			disabled={unavailable}
			aria-label={ariaLabel}
			class="pagination-direction__control"
			onclick={handleClick}
		>
			{#if label}{@render label()}{:else}<span>Next</span>{/if}
			<ChevronRight aria-hidden="true" />
		</Button>
	{:else}
		<Button disabled={unavailable} aria-label={ariaLabel} class="pagination-direction__control" onclick={handleClick}>
			{#if label}{@render label()}{:else}<span>Next</span>{/if}
			<ChevronRight aria-hidden="true" />
		</Button>
	{/if}
</li>

<style>
	.pagination-direction {
		display: flex;
		min-width: 0;
	}

	:global(.pagination-direction__control) {
		padding-inline: 0.75rem;
		white-space: nowrap;
	}
</style>
