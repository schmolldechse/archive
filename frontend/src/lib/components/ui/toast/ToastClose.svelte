<script lang="ts">
	import X from "@lucide/svelte/icons/x";

	import { getToastItemContext } from "./toast-context.svelte";
	import type { ToastCloseProps } from "./types";

	let {
		children,
		ref = $bindable(null),
		class: className,
		disabled = false,
		"aria-label": ariaLabel = "Dismiss notification",
		onclick,
		...restProps
	}: ToastCloseProps = $props();

	const toast = getToastItemContext("ToastClose");

	const handleClick: NonNullable<ToastCloseProps["onclick"]> = (event) => {
		onclick?.(event);
		if (event.defaultPrevented || disabled) return;
		toast.dismiss();
	};
</script>

<button
	{...restProps}
	bind:this={ref}
	type="button"
	class={["toast-close", className]}
	{disabled}
	aria-label={ariaLabel}
	data-toast-close
	onclick={handleClick}
>
	{#if children}{@render children()}{:else}<X aria-hidden="true" />{/if}
</button>

<style>
	.toast-close {
		display: inline-grid;
		width: 2.75rem;
		height: 2.75rem;
		flex: none;
		place-items: center;
		appearance: none;
		border: 1px solid transparent;
		border-radius: var(--radius-control);
		background: transparent;
		color: var(--marginal-note);
		cursor: pointer;
		transition:
			background-color 150ms ease,
			border-color 150ms ease,
			color 150ms ease,
			opacity 150ms ease;
	}

	.toast-close:hover:not(:disabled) {
		border-color: var(--border-trace);
		background: var(--reading-room);
		color: var(--reading-ink);
	}

	.toast-close:disabled {
		cursor: not-allowed;
		opacity: 0.58;
	}

	.toast-close :global(svg) {
		width: 1.25rem;
		height: 1.25rem;
		stroke-width: 1.75;
	}

	@media (prefers-reduced-motion: reduce) {
		.toast-close {
			transition: none;
		}
	}
</style>
