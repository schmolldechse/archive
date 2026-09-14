<script lang="ts">
	import Check from "@lucide/svelte/icons/check";
	import { onDestroy } from "svelte";

	import { getSelectContext } from "./select-context.svelte";
	import type { SelectItemProps, SelectItemState } from "./types";

	const generatedId = $props.id();

	let {
		value,
		textValue,
		disabled = false,
		children,
		ref = $bindable(null),
		class: className,
		id = generatedId,
		onclick,
		onkeydown,
		onpointerdown,
		onpointermove,
		...restProps
	}: SelectItemProps = $props();

	const select = getSelectContext("SelectItem");
	const itemId = $derived(id ?? generatedId);
	const itemValue = $derived(value);
	const isDisabled = $derived(select.disabled || disabled);
	const isSelected = $derived(select.isItemSelected(itemValue));
	const isHighlighted = $derived(select.highlightedId === itemId);
	const state: SelectItemState = $derived({
		selected: isSelected,
		highlighted: isHighlighted,
		disabled: isDisabled
	});
	const unregisterItem = select.registerItem({
		get id() {
			return itemId;
		},
		get value() {
			return itemValue;
		},
		getDisabled: () => isDisabled,
		getElement: () => ref,
		getTextValue: () => textValue ?? ref?.textContent ?? ""
	});

	onDestroy(unregisterItem);

	const handleClick: NonNullable<SelectItemProps["onclick"]> = (event) => {
		onclick?.(event);
		if (event.defaultPrevented || isDisabled) return;

		select.selectItem(itemValue, event.isTrusted ? "pointer" : "programmatic");
	};

	const handleKeydown: NonNullable<SelectItemProps["onkeydown"]> = (event) => {
		onkeydown?.(event);
	};

	const handlePointerDown: NonNullable<SelectItemProps["onpointerdown"]> = (event) => {
		onpointerdown?.(event);
		if (!event.defaultPrevented && !isDisabled && event.button === 0) event.preventDefault();
	};

	const handlePointerMove: NonNullable<SelectItemProps["onpointermove"]> = (event) => {
		onpointermove?.(event);
		if (!event.defaultPrevented && !isDisabled && event.pointerType !== "touch") select.setHighlighted(itemId);
	};
</script>

<div
	{...restProps}
	bind:this={ref}
	id={itemId}
	role="option"
	tabindex={-1}
	class={["select-item", className]}
	aria-selected={isSelected}
	aria-disabled={isDisabled ? "true" : undefined}
	data-select-item
	data-value={itemValue}
	data-selected={isSelected ? "" : undefined}
	data-highlighted={isHighlighted ? "" : undefined}
	data-disabled={isDisabled ? "" : undefined}
	onclick={handleClick}
	onkeydown={handleKeydown}
	onpointerdown={handlePointerDown}
	onpointermove={handlePointerMove}
>
	<span class="select-item__content">{@render children(state)}</span>
	<span class="select-item__indicator" aria-hidden="true" data-visible={isSelected ? "" : undefined}>
		<Check size={18} strokeWidth={2} />
	</span>
</div>

<style>
	.select-item {
		display: flex;
		width: 100%;
		min-height: 2.75rem;
		align-items: center;
		gap: 0.75rem;
		box-sizing: border-box;
		border-inline-start: 2px solid transparent;
		border-radius: var(--radius-control);
		background: transparent;
		padding: 0.625rem 0.75rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 0.9375rem;
		line-height: 1.4;
		text-align: start;
		overflow-wrap: anywhere;
		cursor: pointer;
		user-select: none;
		transition:
			background-color 150ms ease,
			border-color 150ms ease;
	}

	.select-item[data-highlighted] {
		border-inline-start-color: var(--register-mark);
		background: var(--archive-layer);
	}

	.select-item[data-selected] .select-item__content {
		font-weight: 600;
	}

	.select-item[data-disabled] {
		color: var(--marginal-note);
		cursor: not-allowed;
		opacity: 0.68;
	}

	.select-item__content {
		min-width: 0;
		flex: 1;
	}

	.select-item__indicator {
		display: inline-flex;
		width: 1.125rem;
		height: 1.125rem;
		flex: none;
		align-items: center;
		justify-content: center;
		color: var(--register-mark);
		visibility: hidden;
	}

	.select-item__indicator[data-visible] {
		visibility: visible;
	}

	@media (prefers-reduced-motion: reduce) {
		.select-item {
			transition: none;
		}
	}
</style>
