<script lang="ts">
	import CircleCheck from "@lucide/svelte/icons/circle-check";
	import { onDestroy } from "svelte";

	import {
		getDropdownMenuContentContext,
		getDropdownMenuRadioGroupContext,
		getDropdownMenuRootContext
	} from "./dropdown-menu-context.svelte";
	import { createDropdownMenuSelectEvent } from "./helpers";
	import type { DropdownMenuRadioItemProps } from "./types";

	const generatedId = $props.id();

	let {
		value,
		disabled = false,
		textValue,
		closeOnSelect = true,
		onSelect,
		children,
		ref = $bindable(null),
		class: className,
		onclick,
		onfocus,
		onpointermove,
		...restProps
	}: DropdownMenuRadioItemProps = $props();

	const root = getDropdownMenuRootContext("DropdownMenuRadioItem");
	const menu = getDropdownMenuContentContext("DropdownMenuRadioItem");
	const group = getDropdownMenuRadioGroupContext("DropdownMenuRadioItem");
	const checked = $derived(group.value === value);
	const isDisabled = $derived(root.disabled || disabled);
	const isHighlighted = $derived(menu.activeItemId === generatedId);
	const unregisterValue = group.registerValue(() => value);
	const unregisterItem = menu.registerItem({
		id: generatedId,
		getDisabled: () => isDisabled,
		getElement: () => ref,
		getTextValue: () => textValue ?? ref?.textContent ?? ""
	});

	onDestroy(() => {
		unregisterValue();
		unregisterItem();
	});

	const handleClick: NonNullable<DropdownMenuRadioItemProps["onclick"]> = (event) => {
		onclick?.(event);
		if (event.defaultPrevented || isDisabled) return;

		const selectEvent = createDropdownMenuSelectEvent(event);
		onSelect?.(selectEvent);
		if (selectEvent.defaultPrevented) {
			event.preventDefault();
			return;
		}

		group.commit(value);
		if (closeOnSelect) root.requestOpenChange(false, "selection", undefined, true);
	};

	const handleFocus: NonNullable<DropdownMenuRadioItemProps["onfocus"]> = (event) => {
		onfocus?.(event);
		if (!event.defaultPrevented && !isDisabled) menu.activateItem(generatedId);
	};

	const handlePointerMove: NonNullable<DropdownMenuRadioItemProps["onpointermove"]> = (event) => {
		onpointermove?.(event);
		if (!event.defaultPrevented && !isDisabled && event.pointerType !== "touch") menu.activateItem(generatedId, true);
	};
</script>

<button
	{...restProps}
	bind:this={ref}
	type="button"
	role="menuitemradio"
	disabled={isDisabled}
	tabindex={isHighlighted ? 0 : -1}
	class={["dropdown-menu-choice-item", className]}
	aria-checked={checked}
	data-dropdown-menu-radio-item
	data-dropdown-menu-item
	data-value={value}
	data-state={checked ? "checked" : "unchecked"}
	data-checked={checked ? "" : undefined}
	data-highlighted={isHighlighted ? "" : undefined}
	data-disabled={isDisabled ? "" : undefined}
	onclick={handleClick}
	onfocus={handleFocus}
	onpointermove={handlePointerMove}
>
	<span class="dropdown-menu-choice-item__indicator" aria-hidden="true">
		{#if checked}<CircleCheck size={16} strokeWidth={2} />{/if}
	</span>
	<span class="dropdown-menu-choice-item__label">{@render children({ checked })}</span>
</button>

<style>
	.dropdown-menu-choice-item {
		display: grid;
		width: 100%;
		min-height: 2.75rem;
		grid-template-columns: 1rem minmax(0, 1fr);
		align-items: center;
		gap: 0.75rem;
		box-sizing: border-box;
		border: 0;
		border-inline-start: 2px solid transparent;
		border-radius: var(--radius-control);
		background: transparent;
		padding: 0.625rem 0.75rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 0.9375rem;
		line-height: 1.35;
		text-align: start;
		overflow-wrap: anywhere;
		cursor: pointer;
		transition:
			background-color 150ms ease,
			border-color 150ms ease;
	}

	.dropdown-menu-choice-item[data-highlighted] {
		border-inline-start-color: var(--register-mark);
		background: var(--reading-room);
	}

	.dropdown-menu-choice-item[data-state="checked"] {
		font-weight: 600;
	}

	.dropdown-menu-choice-item[data-disabled] {
		color: var(--marginal-note);
		cursor: not-allowed;
		opacity: 0.68;
	}

	.dropdown-menu-choice-item__indicator {
		display: inline-flex;
		width: 1rem;
		color: var(--register-mark);
	}

	.dropdown-menu-choice-item__label {
		min-width: 0;
	}

	@media (prefers-reduced-motion: reduce) {
		.dropdown-menu-choice-item {
			transition: none;
		}
	}
</style>
