<script lang="ts">
	import ChevronLeft from "@lucide/svelte/icons/chevron-left";

	import Button from "../Button.svelte";
	import { getPaginationContext } from "./pagination-context.svelte";
	import type { PaginationPreviousProps } from "./types";

	let {
		label,
		"aria-label": ariaLabel = "Previous page",
		ref = $bindable(null),
		class: className,
		...restProps
	}: PaginationPreviousProps = $props();

	const pagination = getPaginationContext("PaginationPrevious");
	const destination = $derived(Math.max(pagination.page - 1, 1));
	const unavailable = $derived(pagination.isPageUnavailable(destination) || pagination.page <= 1);
	const handleClick = (event: MouseEvent): void => pagination.select(destination, event);
</script>

<li
	{...restProps}
	bind:this={ref}
	class={["pagination-direction", className]}
	data-pagination-previous
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
			<ChevronLeft aria-hidden="true" />
			{#if label}{@render label()}{:else}<span>Previous</span>{/if}
		</Button>
	{:else}
		<Button disabled={unavailable} aria-label={ariaLabel} class="pagination-direction__control" onclick={handleClick}>
			<ChevronLeft aria-hidden="true" />
			{#if label}{@render label()}{:else}<span>Previous</span>{/if}
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
