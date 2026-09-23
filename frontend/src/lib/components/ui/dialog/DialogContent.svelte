<script lang="ts">
	import { onDestroy } from "svelte";

	import { createDialogAttachment } from "./dialog-attachment.svelte";
	import { getDialogContext } from "./dialog-context.svelte";
	import type { DialogContentProps, DialogSide } from "./types";

	const generatedId = $props.id();

	let {
		size = "medium",
		position = "viewport",
		side = "bottom",
		align = "start",
		sideOffset = 8,
		collisionPadding = 12,
		closeOnEscape = true,
		closeOnOutsidePointer,
		preventScroll,
		restoreFocus = true,
		initialFocus,
		returnFocus,
		children,
		ref = $bindable(null),
		class: className,
		id,
		"aria-label": ariaLabel,
		"aria-labelledby": ariaLabelledby,
		"aria-describedby": ariaDescribedby,
		...restProps
	}: DialogContentProps = $props();

	const dialog = getDialogContext("DialogContent");
	const unregisterId = dialog.registerContentId(() => id ?? generatedId);
	let effectiveModal = $state(dialog.modal);
	let resolvedSide = $state<DialogSide | null>(null);

	onDestroy(unregisterId);

	const dialogAttachment = createDialogAttachment({
		getAlign: () => align,
		getCollisionPadding: () => collisionPadding,
		getCloseOnEscape: () => closeOnEscape,
		getCloseOnOutsidePointer: (modal) => closeOnOutsidePointer ?? modal,
		getInitialFocus: () => initialFocus,
		getModal: () => dialog.modal,
		getOpen: () => dialog.open,
		getPosition: () => position,
		getPreventScroll: (modal) => preventScroll ?? modal,
		getRestoreFocus: () => restoreFocus,
		getReturnFocus: () => returnFocus,
		getSide: () => side,
		getSideOffset: () => sideOffset,
		getTitleId: () => dialog.titleId,
		getTrigger: dialog.getTrigger,
		requestOpenChange: dialog.requestOpenChange,
		setEffectiveModal: (modal) => (effectiveModal = modal),
		setResolvedSide: (nextSide) => (resolvedSide = nextSide),
		synchronizeProgrammaticChange: dialog.synchronizeProgrammaticChange
	});
</script>

<dialog
	{...restProps}
	bind:this={ref}
	id={id ?? generatedId}
	class={["dialog-content", className]}
	aria-label={ariaLabel}
	aria-labelledby={ariaLabelledby ?? dialog.titleId}
	aria-describedby={ariaDescribedby ?? dialog.descriptionId}
	aria-modal={effectiveModal ? "true" : undefined}
	data-component="dialog"
	data-dialog-content
	data-state={dialog.open ? "open" : "closed"}
	data-modal={effectiveModal ? "true" : "false"}
	data-size={size}
	data-position={position}
	data-side={resolvedSide ?? side}
	{@attach dialogAttachment}
>
	{@render children()}
</dialog>

<style>
	.dialog-content {
		width: calc(100% - 2rem);
		max-height: min(80dvh, calc(100% - 2rem));
		box-sizing: border-box;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-preview);
		background: var(--reading-room);
		box-shadow: var(--overlay-shadow);
		padding: 1.5rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 1.0625rem;
		line-height: 1.59;
		opacity: 0;
		transform: translateY(0.5rem);
		transition:
			display 150ms allow-discrete,
			overlay 150ms allow-discrete,
			opacity 150ms ease,
			transform 150ms ease;
	}

	.dialog-content[open] {
		opacity: 1;
		transform: translateY(0);
	}

	.dialog-content[data-size="small"] {
		max-width: 24rem;
	}

	.dialog-content[data-size="medium"] {
		max-width: 36rem;
	}

	.dialog-content[data-size="large"] {
		max-width: 52rem;
	}

	.dialog-content[data-size="viewport"] {
		width: calc(100% - 2rem);
		max-width: none;
		max-height: calc(100dvh - 2rem);
	}

	.dialog-content[data-modal="false"][data-position="viewport"] {
		position: fixed;
		inset: 1.5rem 1.5rem auto auto;
		margin: 0;
		border-width: 2px;
		border-color: var(--reading-ink);
	}

	.dialog-content[data-modal="false"][data-position="trigger"] {
		position: fixed;
		z-index: 20;
		inset: 0 auto auto 0;
		margin: 0;
	}

	.dialog-content::backdrop {
		background: var(--overlay-backdrop);
		opacity: 0;
		transition:
			display 150ms allow-discrete,
			overlay 150ms allow-discrete,
			opacity 150ms ease;
	}

	.dialog-content[open]::backdrop {
		opacity: 1;
	}

	@starting-style {
		.dialog-content[open] {
			opacity: 0;
			transform: translateY(0.5rem);
		}

		.dialog-content[open]::backdrop {
			opacity: 0;
		}
	}

	@media (max-width: 36rem) {
		.dialog-content {
			padding: 1rem;
		}

		.dialog-content[data-modal="false"][data-position="viewport"] {
			inset: 1rem 1rem auto;
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.dialog-content,
		.dialog-content::backdrop {
			transition: none;
		}
	}
</style>
