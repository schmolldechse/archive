<script lang="ts">
	import { onDestroy } from "svelte";

	import { createDropdownMenuAttachment } from "./dropdown-menu-attachment.svelte";
	import {
		createDropdownMenuContentContext,
		getDropdownMenuRootContext,
		setDropdownMenuContentContext
	} from "./dropdown-menu-context.svelte";
	import type { DropdownMenuAlign, DropdownMenuContentProps, DropdownMenuSide } from "./types";

	const generatedId = $props.id();

	let {
		side = "bottom",
		align = "start",
		sideOffset = 8,
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
	}: DropdownMenuContentProps = $props();

	const root = getDropdownMenuRootContext("DropdownMenuContent");
	const menu = createDropdownMenuContentContext({
		root,
		isSubmenu: false,
		closeCurrent: (reason, restoreFocus) => root.requestOpenChange(false, reason, undefined, restoreFocus)
	});
	setDropdownMenuContentContext(menu);

	const unregisterContent = root.registerContent(() => ref);
	const unregisterId = root.registerContentId(() => id ?? generatedId);
	const unregisterMenu = root.registerMenu(() => menu);
	let resolvedSide = $state<DropdownMenuSide>("bottom");
	let resolvedAlign = $state<DropdownMenuAlign>("start");

	onDestroy(() => {
		unregisterContent();
		unregisterId();
		unregisterMenu();
		menu.destroy();
	});

	const contentAttachment = createDropdownMenuAttachment({
		getAlign: () => align,
		getAlignOffset: () => alignOffset,
		getAnchor: root.getTrigger,
		getCollisionPadding: () => collisionPadding,
		getOpen: () => root.open,
		getSide: () => side,
		getSideOffset: () => sideOffset,
		containsTarget: (target) => Boolean(root.getContent()?.contains(target) || root.getTrigger()?.contains(target)),
		onClosed() {
			menu.reset();
			if (root.consumeRestoreFocus()) root.restoreTriggerFocus();
		},
		onNativeClose: () => root.requestOpenChange(false, "native"),
		onOpened() {
			const focusIntent = root.consumeFocusIntent();
			if (focusIntent === "last") menu.focusLast();
			else menu.focusFirst();
		},
		onOutsidePointer: () => root.requestOpenChange(false, "outside"),
		setResolvedPosition(nextSide, nextAlign) {
			resolvedSide = nextSide;
			resolvedAlign = nextAlign;
		},
		synchronizeProgrammaticChange: root.synchronizeProgrammaticChange
	});

	const handleKeydown: NonNullable<DropdownMenuContentProps["onkeydown"]> = (event) => {
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
	class={["dropdown-menu-content", className]}
	aria-label={ariaLabel}
	aria-labelledby={ariaLabelledby ?? (ariaLabel ? undefined : root.triggerId)}
	data-component="dropdown-menu"
	data-dropdown-menu-content
	data-state={root.open ? "open" : "closed"}
	data-side={resolvedSide}
	data-align={resolvedAlign}
	aria-orientation="vertical"
	onkeydown={handleKeydown}
	{@attach contentAttachment}
>
	{@render children()}
</div>

<style>
	.dropdown-menu-content {
		position: fixed;
		inset: 0 auto auto 0;
		width: max-content;
		min-width: 12rem;
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

	.dropdown-menu-content:popover-open {
		opacity: 1;
	}

	@starting-style {
		.dropdown-menu-content:popover-open {
			opacity: 0;
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.dropdown-menu-content {
			transition: none;
		}
	}
</style>
