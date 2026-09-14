<script lang="ts">
	import { createSelectAttachment } from "./select-attachment.svelte";
	import { getSelectContext } from "./select-context.svelte";
	import type { SelectAlign, SelectContentProps, SelectSide } from "./types";

	const generatedId = $props.id();

	let {
		side = "bottom",
		align = "start",
		sideOffset = 6,
		matchTriggerWidth = true,
		collisionPadding = 12,
		children,
		ref = $bindable(null),
		class: className,
		id = generatedId,
		...restProps
	}: SelectContentProps = $props();

	const select = getSelectContext("SelectContent");
	let resolvedSide = $state<SelectSide | null>(null);
	let resolvedAlign = $state<SelectAlign | null>(null);

	const contentAttachment = createSelectAttachment({
		getAlign: () => align,
		getAnchor: select.getTrigger,
		getCollisionPadding: () => collisionPadding,
		getMatchTriggerWidth: () => matchTriggerWidth,
		getOpen: () => select.open,
		getSide: () => side,
		getSideOffset: () => sideOffset,
		containsTarget: (target) => Boolean(ref?.contains(target) || select.getTrigger()?.contains(target)),
		onClosed() {
			if (select.consumeRestoreFocus()) select.restoreTriggerFocus();
		},
		onNativeClose: () => select.requestOpenChange(false, undefined, true),
		onOpened() {
			select.reconcileItems();
			select.focusViewport();
		},
		onOutsidePointer: () => select.requestOpenChange(false),
		reconcileItems: select.reconcileItems,
		setResolvedPosition(nextSide, nextAlign) {
			resolvedSide = nextSide;
			resolvedAlign = nextAlign;
		},
		synchronizeProgrammaticOpen: select.synchronizeProgrammaticOpen
	});
</script>

<div
	{...restProps}
	bind:this={ref}
	{id}
	popover="manual"
	hidden={!select.open}
	inert={!select.open}
	class={["select-content", className]}
	aria-hidden={select.open ? undefined : "true"}
	data-select-content
	data-state={select.open ? "open" : "closed"}
	data-side={resolvedSide ?? side}
	data-align={resolvedAlign ?? align}
	{@attach contentAttachment}
>
	{@render children()}
</div>

<style>
	.select-content {
		position: fixed;
		inset: 0 auto auto 0;
		width: max-content;
		min-width: 12rem;
		max-width: min(24rem, calc(100vw - 1.5rem));
		box-sizing: border-box;
		margin: 0;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--reading-room);
		box-shadow: var(--overlay-shadow);
		padding: 0.25rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 0.9375rem;
		line-height: 1.4;
		opacity: 0;
		transform: translateY(-0.25rem);
		transition:
			display 150ms allow-discrete,
			overlay 150ms allow-discrete,
			opacity 150ms ease,
			transform 150ms ease;
	}

	.select-content[data-state="open"] {
		opacity: 1;
		transform: translateY(0);
	}

	.select-content[data-side="top"] {
		transform: translateY(0.25rem);
	}

	.select-content[data-side="top"][data-state="open"] {
		transform: translateY(0);
	}

	@starting-style {
		.select-content[data-state="open"] {
			opacity: 0;
			transform: translateY(-0.25rem);
		}

		.select-content[data-side="top"][data-state="open"] {
			transform: translateY(0.25rem);
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.select-content {
			transition: none;
		}
	}
</style>
