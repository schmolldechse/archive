<script lang="ts">
	import { onDestroy } from "svelte";

	import { createDropdownMenuAttachment } from "./dropdown-menu-attachment.svelte";
	import {
		createDropdownMenuContentContext,
		getDropdownMenuRootContext,
		getDropdownMenuSubContext,
		setDropdownMenuContentContext
	} from "./dropdown-menu-context.svelte";
	import type { DropdownMenuAlign, DropdownMenuSide, DropdownMenuSubContentProps } from "./types";

	const generatedId = $props.id();

	let {
		side,
		align = "start",
		sideOffset = 4,
		alignOffset = 0,
		collisionPadding = 8,
		children,
		ref = $bindable(null),
		class: className,
		id,
		"aria-label": ariaLabel,
		"aria-labelledby": ariaLabelledby,
		onkeydown,
		...restProps
	}: DropdownMenuSubContentProps = $props();

	const root = getDropdownMenuRootContext("DropdownMenuSubContent");
	const submenu = getDropdownMenuSubContext("DropdownMenuSubContent");
	const menu = createDropdownMenuContentContext({
		root,
		isSubmenu: true,
		closeCurrent: (reason, restoreFocus) => submenu.requestOpenChange(false, reason, undefined, restoreFocus)
	});
	setDropdownMenuContentContext(menu);

	const unregisterContent = submenu.registerContent(() => ref);
	const unregisterId = submenu.registerContentId(() => id ?? generatedId);
	const unregisterMenu = submenu.registerMenu(() => menu);
	let resolvedSide = $state<DropdownMenuSide>("right");
	let resolvedAlign = $state<DropdownMenuAlign>("start");

	onDestroy(() => {
		unregisterContent();
		unregisterId();
		unregisterMenu();
		menu.destroy();
	});

	const preferredSide = (): DropdownMenuSide => {
		if (side) return side;
		const trigger = submenu.getTrigger();
		return trigger && getComputedStyle(trigger).direction === "rtl" ? "left" : "right";
	};

	const contentAttachment = createDropdownMenuAttachment({
		getAlign: () => align,
		getAlignOffset: () => alignOffset,
		getAnchor: submenu.getTrigger,
		getCollisionPadding: () => collisionPadding,
		getOpen: () => submenu.open && root.open,
		getSide: preferredSide,
		getSideOffset: () => sideOffset,
		onClosed() {
			menu.reset();
			if (submenu.consumeRestoreFocus()) submenu.restoreTriggerFocus();
		},
		onNativeClose: () => submenu.requestOpenChange(false, "native"),
		onOpened() {
			const focusIntent = submenu.consumeFocusIntent();
			if (focusIntent === "last") menu.focusLast();
			else if (focusIntent === "first") menu.focusFirst();
		},
		setResolvedPosition(nextSide, nextAlign) {
			resolvedSide = nextSide;
			resolvedAlign = nextAlign;
		},
		synchronizeProgrammaticChange: submenu.synchronizeProgrammaticChange
	});

	const handleKeydown: NonNullable<DropdownMenuSubContentProps["onkeydown"]> = (event) => {
		onkeydown?.(event);
		if (event.defaultPrevented) return;

		const target = event.target;
		if (target instanceof Element && target.closest("[data-dropdown-menu-content]") !== event.currentTarget) return;
		menu.handleKeydown(event);
	};
</script>

<div
	{...restProps}
	bind:this={ref}
	id={id ?? generatedId}
	popover="manual"
	role="menu"
	class={["dropdown-menu-sub-content", className]}
	aria-label={ariaLabel}
	aria-labelledby={ariaLabelledby ?? (ariaLabel ? undefined : submenu.triggerId)}
	aria-orientation="vertical"
	data-dropdown-menu-content
	data-dropdown-menu-sub-content
	data-state={submenu.open ? "open" : "closed"}
	data-side={resolvedSide}
	data-align={resolvedAlign}
	onkeydown={handleKeydown}
	{@attach contentAttachment}
>
	{@render children()}
</div>

<style>
	.dropdown-menu-sub-content {
		position: fixed;
		inset: 0 auto auto 0;
		width: max-content;
		min-width: 11rem;
		max-width: min(22rem, calc(100vw - 1rem));
		max-height: calc(100dvh - 1rem);
		box-sizing: border-box;
		margin: 0;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		box-shadow: var(--overlay-shadow);
		padding: 0.25rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 0.9375rem;
		line-height: 1.35;
		overflow: auto;
		opacity: 0;
		transition:
			display 150ms allow-discrete,
			overlay 150ms allow-discrete,
			opacity 150ms ease;
	}

	.dropdown-menu-sub-content:popover-open {
		opacity: 1;
	}

	@starting-style {
		.dropdown-menu-sub-content:popover-open {
			opacity: 0;
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.dropdown-menu-sub-content {
			transition: none;
		}
	}
</style>
