<script lang="ts">
	import { onDestroy } from "svelte";

	import { getToastItemContext } from "./toast-context.svelte";
	import type { ToastDescriptionProps } from "./types";

	let { children, ref = $bindable(null), class: className, id, ...restProps }: ToastDescriptionProps = $props();
	const toast = getToastItemContext("ToastDescription");
	const resolvedId = $derived(id ?? toast.defaultDescriptionId);
	const unregisterId = toast.registerDescriptionId(() => resolvedId);

	onDestroy(unregisterId);
</script>

<div {...restProps} bind:this={ref} id={resolvedId} class={["toast-description", className]} data-toast-description>
	{#if children}{@render children()}{:else}{toast.record.description}{/if}
</div>

<style>
	.toast-description {
		margin-block-start: 0.25rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.43;
		overflow-wrap: anywhere;
	}
</style>
