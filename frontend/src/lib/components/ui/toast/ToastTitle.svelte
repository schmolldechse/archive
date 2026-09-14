<script lang="ts">
	import { onDestroy } from "svelte";

	import { getToastItemContext } from "./toast-context.svelte";
	import type { ToastTitleProps } from "./types";

	let { children, ref = $bindable(null), class: className, id, ...restProps }: ToastTitleProps = $props();
	const toast = getToastItemContext("ToastTitle");
	const resolvedId = $derived(id ?? toast.defaultTitleId);
	const unregisterId = toast.registerTitleId(() => resolvedId);

	onDestroy(unregisterId);
</script>

<div {...restProps} bind:this={ref} id={resolvedId} class={["toast-title", className]} data-toast-title>
	{#if children}{@render children()}{:else}{toast.record.title}{/if}
</div>

<style>
	.toast-title {
		margin: 0;
		color: var(--reading-ink);
		font-size: 0.9375rem;
		font-weight: 600;
		line-height: 1.35;
		overflow-wrap: anywhere;
	}
</style>
