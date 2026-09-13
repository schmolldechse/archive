<script lang="ts">
	import ChevronRight from "@lucide/svelte/icons/chevron-right";
	import { onDestroy } from "svelte";

	import {
		getDropdownMenuContentContext,
		getDropdownMenuRootContext,
		getDropdownMenuSubContext
	} from "./dropdown-menu-context.svelte";
	import type { DropdownMenuSubTriggerProps } from "./types";

	const generatedId = $props.id();

	let {
		disabled = false,
		textValue,
		openDelay = 100,
		children,
		ref = $bindable(null),
		class: className,
		id,
		onclick,
		onfocus,
		onpointerleave,
		onpointermove,
		...restProps
	}: DropdownMenuSubTriggerProps = $props();

	const root = getDropdownMenuRootContext("DropdownMenuSubTrigger");
	const menu = getDropdownMenuContentContext("DropdownMenuSubTrigger");
	const submenu = getDropdownMenuSubContext("DropdownMenuSubTrigger");
	const isDisabled = $derived(root.disabled || disabled);
	const isHighlighted = $derived(menu.activeItemId === generatedId);
	let openTimer: ReturnType<typeof setTimeout> | undefined;

	const clearOpenTimer = () => {
		if (!openTimer) return;
		clearTimeout(openTimer);
		openTimer = undefined;
	};

	const unregisterTrigger = submenu.registerTrigger(() => ref);
	const unregisterId = submenu.registerTriggerId(() => id ?? generatedId);
	const unregisterItem = menu.registerItem({
		id: generatedId,
		submenuId: submenu.id,
		getDisabled: () => isDisabled,
		getElement: () => ref,
		getTextValue: () => textValue ?? ref?.textContent ?? "",
		openSubmenu: () => menu.openSubmenu(submenu.id, "keyboard", "first")
	});

	onDestroy(() => {
		clearOpenTimer();
		unregisterTrigger();
		unregisterId();
		unregisterItem();
	});

	const handleClick: NonNullable<DropdownMenuSubTriggerProps["onclick"]> = (event) => {
		onclick?.(event);
		if (event.defaultPrevented || isDisabled) return;

		clearOpenTimer();
		menu.activateItem(generatedId);
		if (submenu.open) submenu.requestOpenChange(false, "trigger");
		else menu.openSubmenu(submenu.id, "trigger", "first");
	};

	const handleFocus: NonNullable<DropdownMenuSubTriggerProps["onfocus"]> = (event) => {
		onfocus?.(event);
		if (!event.defaultPrevented && !isDisabled) {
			clearOpenTimer();
			menu.activateItem(generatedId);
		}
	};

	const handlePointerLeave: NonNullable<DropdownMenuSubTriggerProps["onpointerleave"]> = (event) => {
		onpointerleave?.(event);
		clearOpenTimer();
	};

	const handlePointerMove: NonNullable<DropdownMenuSubTriggerProps["onpointermove"]> = (event) => {
		onpointermove?.(event);
		if (event.defaultPrevented || isDisabled || event.pointerType === "touch") return;

		menu.activateItem(generatedId, true);
		if (submenu.open || (event.movementX === 0 && event.movementY === 0)) return;
		clearOpenTimer();
		openTimer = setTimeout(() => {
			openTimer = undefined;
			if (!root.open || isDisabled || menu.activeItemId !== generatedId) return;
			menu.openSubmenu(submenu.id, "pointer");
		}, Math.max(0, openDelay));
	};
</script>

<button
	{...restProps}
	bind:this={ref}
	id={id ?? generatedId}
	type="button"
	role="menuitem"
	disabled={isDisabled}
	tabindex={isHighlighted ? 0 : -1}
	class={["dropdown-menu-sub-trigger", className]}
	aria-haspopup="menu"
	aria-expanded={submenu.open}
	aria-controls={submenu.contentId}
	data-dropdown-menu-sub-trigger
	data-dropdown-menu-item
	data-state={submenu.open ? "open" : "closed"}
	data-highlighted={isHighlighted ? "" : undefined}
	data-disabled={isDisabled ? "" : undefined}
	onclick={handleClick}
	onfocus={handleFocus}
	onpointerleave={handlePointerLeave}
	onpointermove={handlePointerMove}
>
	<span class="dropdown-menu-sub-trigger__label">{@render children()}</span>
	<span class="dropdown-menu-sub-trigger__icon" aria-hidden="true">
		<ChevronRight size={16} strokeWidth={1.75} />
	</span>
</button>

<style>
	.dropdown-menu-sub-trigger {
		display: grid;
		width: 100%;
		min-height: 2.75rem;
		grid-template-columns: minmax(0, 1fr) 1rem;
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

	.dropdown-menu-sub-trigger[data-highlighted],
	.dropdown-menu-sub-trigger[data-state="open"] {
		border-inline-start-color: var(--register-mark);
		background: var(--reading-room);
	}

	.dropdown-menu-sub-trigger[data-disabled] {
		color: var(--marginal-note);
		cursor: not-allowed;
		opacity: 0.68;
	}

	.dropdown-menu-sub-trigger__label {
		min-width: 0;
	}

	.dropdown-menu-sub-trigger__icon {
		display: inline-flex;
		justify-content: center;
	}

	.dropdown-menu-sub-trigger:dir(rtl) .dropdown-menu-sub-trigger__icon {
		transform: scaleX(-1);
	}

	@media (prefers-reduced-motion: reduce) {
		.dropdown-menu-sub-trigger {
			transition: none;
		}
	}
</style>
