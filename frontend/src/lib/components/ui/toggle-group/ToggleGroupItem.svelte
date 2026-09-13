<script lang="ts">
	import { onDestroy } from "svelte";

	import { mergeClasses } from "./helpers";
	import { getToggleGroupContext } from "./toggle-group-context.svelte";
	import type { ToggleGroupItemProps } from "./types";

	let {
		value,
		disabled = false,
		children,
		ref = $bindable(null),
		class: className,
		onclick,
		onfocus,
		onblur,
		onkeydown,
		...restProps
	}: ToggleGroupItemProps = $props();

	const group = getToggleGroupContext();
	let highlighted = $state(false);
	const pressed = $derived(group.isPressed(value));
	const isDisabled = $derived(group.isItemDisabled(value, disabled));
	const tabIndex = $derived(group.getTabIndex(value, disabled));

	const unregister = group.registerItem({
		get disabled() {
			return disabled;
		},
		get element() {
			return ref;
		},
		get value() {
			return value;
		}
	});

	onDestroy(unregister);

	const handleClick = (event: Parameters<NonNullable<ToggleGroupItemProps["onclick"]>>[0]): void => {
		onclick?.(event);
		if (!event.defaultPrevented) group.toggle(value, disabled);
	};

	const handleFocus = (event: Parameters<NonNullable<ToggleGroupItemProps["onfocus"]>>[0]): void => {
		onfocus?.(event);
		if (event.defaultPrevented) return;

		highlighted = true;
		group.focusItem(value, disabled);
	};

	const handleBlur = (event: Parameters<NonNullable<ToggleGroupItemProps["onblur"]>>[0]): void => {
		onblur?.(event);
		if (!event.defaultPrevented) highlighted = false;
	};

	const handleKeydown = (event: Parameters<NonNullable<ToggleGroupItemProps["onkeydown"]>>[0]): void => {
		onkeydown?.(event);
		if (!event.defaultPrevented) group.navigate(event, value);
	};
</script>

<button
	{...restProps}
	bind:this={ref}
	type="button"
	disabled={isDisabled}
	class={mergeClasses("toggle-group-item", className)}
	aria-pressed={pressed}
	tabindex={tabIndex}
	data-toggle-group-item
	data-value={value}
	data-state={pressed ? "on" : "off"}
	data-disabled={isDisabled ? "" : undefined}
	data-highlighted={highlighted ? "" : undefined}
	onclick={handleClick}
	onfocus={handleFocus}
	onblur={handleBlur}
	onkeydown={handleKeydown}
>
	{@render children({ pressed })}
</button>

<style>
	.toggle-group-item {
		position: relative;
		display: inline-flex;
		min-width: 2.75rem;
		max-width: 100%;
		min-height: 2.75rem;
		flex: none;
		align-items: center;
		justify-content: center;
		box-sizing: border-box;
		border: 1px solid var(--border-trace);
		border-radius: 0;
		background: transparent;
		padding: 0.6875rem 0.75rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 0.9375rem;
		font-weight: 400;
		line-height: 1.25rem;
		text-align: center;
		overflow-wrap: anywhere;
		cursor: pointer;
		transition:
			background-color 150ms ease,
			border-color 150ms ease,
			box-shadow 150ms ease,
			color 150ms ease;
	}

	.toggle-group-item:hover:not(:disabled) {
		z-index: 1;
		border-color: var(--reading-ink);
		background: var(--archive-layer);
	}

	.toggle-group-item[data-state="on"] {
		z-index: 1;
		border-color: var(--register-mark);
		box-shadow: inset 0 -3px 0 var(--register-mark);
		color: var(--register-mark);
		font-weight: 600;
	}

	.toggle-group-item:focus-visible {
		z-index: 2;
	}

	.toggle-group-item:disabled {
		color: var(--marginal-note);
		cursor: not-allowed;
		opacity: 0.58;
	}

	.toggle-group-item :global(svg) {
		width: 1.25rem;
		height: 1.25rem;
		flex: none;
		stroke-width: 1.75;
	}

	@media (prefers-reduced-motion: reduce) {
		.toggle-group-item {
			transition: none;
		}
	}
</style>
