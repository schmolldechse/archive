<script lang="ts">
	import Button from "../Button.svelte";
	import { getPaginationContext } from "./pagination-context.svelte";
	import type { PaginationPageProps } from "./types";

	let { item, "aria-label": ariaLabel, ref = $bindable(null), class: className, ...restProps }: PaginationPageProps = $props();

	const pagination = getPaginationContext("PaginationPage");
	const state = $derived(pagination.getPageState(item.value));
	const unavailable = $derived(state === "disabled");
	const current = $derived(item.value === pagination.page);
	const accessibleLabel = $derived(ariaLabel ?? `Page ${item.value}`);
	const handleClick = (event: MouseEvent): void => pagination.select(item.value, event);
</script>

<li
	{...restProps}
	bind:this={ref}
	class={["pagination-page", className]}
	data-pagination-page
	data-page={item.value}
	data-state={state}
>
	{#if pagination.mode === "link"}
		<Button
			href={pagination.getHref(item.value)}
			disabled={unavailable}
			aria-label={accessibleLabel}
			aria-current={current ? "page" : undefined}
			class="pagination-page__control"
			onclick={handleClick}
		>
			{item.value}
		</Button>
	{:else}
		<Button
			disabled={unavailable}
			aria-label={accessibleLabel}
			aria-current={current ? "page" : undefined}
			class="pagination-page__control"
			onclick={handleClick}
		>
			{item.value}
		</Button>
	{/if}
</li>

<style>
	.pagination-page {
		display: flex;
	}

	:global(.pagination-page__control) {
		position: relative;
		padding-inline: 0.75rem;
		font-variant-numeric: tabular-nums;
	}

	:global(.pagination-page__control[aria-current="page"]) {
		border-color: var(--register-mark);
		color: var(--register-mark);
		font-weight: 600;
	}

	:global(.pagination-page__control[aria-current="page"])::after {
		position: absolute;
		inset-inline: 0.375rem;
		inset-block-end: -1px;
		height: 3px;
		border-radius: var(--radius-mark);
		background: var(--register-mark);
		content: "";
	}
</style>
