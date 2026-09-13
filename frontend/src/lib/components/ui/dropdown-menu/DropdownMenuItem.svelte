<script lang="ts">
	import { onDestroy } from "svelte";
	import type { HTMLAnchorAttributes, HTMLButtonAttributes } from "svelte/elements";

	import {
		getDropdownMenuContentContext,
		getDropdownMenuRootContext
	} from "./dropdown-menu-context.svelte";
	import { createDropdownMenuSelectEvent } from "./helpers";
	import type { DropdownMenuItemProps } from "./types";

	const generatedId = $props.id();

	let {
		href,
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
	}: DropdownMenuItemProps = $props();

	const root = getDropdownMenuRootContext("DropdownMenuItem");
	const menu = getDropdownMenuContentContext("DropdownMenuItem");
	const isDisabled = $derived(root.disabled || disabled);
	const isHighlighted = $derived(menu.activeItemId === generatedId);
	const unregisterItem = menu.registerItem({
		id: generatedId,
		getDisabled: () => isDisabled,
		getElement: () => ref,
		getTextValue: () => textValue ?? ref?.textContent ?? ""
	});

	onDestroy(unregisterItem);

	const select = (event: MouseEvent) => {
		if (isDisabled) {
			event.preventDefault();
			return;
		}

		const selectEvent = createDropdownMenuSelectEvent(event);
		onSelect?.(selectEvent);
		if (selectEvent.defaultPrevented) {
			event.preventDefault();
			return;
		}

		if (closeOnSelect) root.requestOpenChange(false, "selection", undefined, href === undefined);
	};

	const handleButtonClick: NonNullable<HTMLButtonAttributes["onclick"]> = (event) => {
		(onclick as HTMLButtonAttributes["onclick"])?.(event);
		if (!event.defaultPrevented) select(event);
	};

	const handleAnchorClick: NonNullable<HTMLAnchorAttributes["onclick"]> = (event) => {
		(onclick as HTMLAnchorAttributes["onclick"])?.(event);
		if (!event.defaultPrevented) select(event);
	};

	const handleButtonFocus: NonNullable<HTMLButtonAttributes["onfocus"]> = (event) => {
		(onfocus as HTMLButtonAttributes["onfocus"])?.(event);
		if (!event.defaultPrevented && !isDisabled) menu.activateItem(generatedId);
	};

	const handleAnchorFocus: NonNullable<HTMLAnchorAttributes["onfocus"]> = (event) => {
		(onfocus as HTMLAnchorAttributes["onfocus"])?.(event);
		if (!event.defaultPrevented && !isDisabled) menu.activateItem(generatedId);
	};

	const handleButtonPointerMove: NonNullable<HTMLButtonAttributes["onpointermove"]> = (event) => {
		(onpointermove as HTMLButtonAttributes["onpointermove"])?.(event);
		if (!event.defaultPrevented && !isDisabled && event.pointerType !== "touch") menu.activateItem(generatedId, true);
	};

	const handleAnchorPointerMove: NonNullable<HTMLAnchorAttributes["onpointermove"]> = (event) => {
		(onpointermove as HTMLAnchorAttributes["onpointermove"])?.(event);
		if (!event.defaultPrevented && !isDisabled && event.pointerType !== "touch") menu.activateItem(generatedId, true);
	};
</script>

{#if href !== undefined}
	<a
		{...restProps as Omit<HTMLAnchorAttributes, "children">}
		bind:this={ref}
		href={isDisabled ? undefined : href}
		role="menuitem"
		tabindex={isDisabled ? -1 : (isHighlighted ? 0 : -1)}
		class={["dropdown-menu-item", className]}
		aria-disabled={isDisabled ? "true" : undefined}
		data-dropdown-menu-item
		data-highlighted={isHighlighted ? "" : undefined}
		data-disabled={isDisabled ? "" : undefined}
		onclick={handleAnchorClick}
		onfocus={handleAnchorFocus}
		onpointermove={handleAnchorPointerMove}
	>
		{@render children()}
	</a>
{:else}
	<button
		{...restProps as Omit<HTMLButtonAttributes, "children">}
		bind:this={ref}
		type="button"
		role="menuitem"
		disabled={isDisabled}
		tabindex={isHighlighted ? 0 : -1}
		class={["dropdown-menu-item", className]}
		data-dropdown-menu-item
		data-highlighted={isHighlighted ? "" : undefined}
		data-disabled={isDisabled ? "" : undefined}
		onclick={handleButtonClick}
		onfocus={handleButtonFocus}
		onpointermove={handleButtonPointerMove}
	>
		{@render children()}
	</button>
{/if}

<style>
	.dropdown-menu-item {
		display: flex;
		width: 100%;
		min-height: 2.75rem;
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
		text-decoration: none;
		overflow-wrap: anywhere;
		cursor: pointer;
		transition:
			background-color 150ms ease,
			border-color 150ms ease;
	}

	button.dropdown-menu-item {
		appearance: none;
	}

	.dropdown-menu-item[data-highlighted] {
		border-inline-start-color: var(--register-mark);
		background: var(--reading-room);
	}

	.dropdown-menu-item[data-disabled] {
		color: var(--marginal-note);
		cursor: not-allowed;
		opacity: 0.68;
	}

	@media (prefers-reduced-motion: reduce) {
		.dropdown-menu-item {
			transition: none;
		}
	}
</style>
