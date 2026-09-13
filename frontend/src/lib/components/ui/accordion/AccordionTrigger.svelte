<script lang="ts">
	import ChevronDown from "@lucide/svelte/icons/chevron-down";
	import { onDestroy } from "svelte";

	import { getAccordionItemContext } from "./accordion-context.svelte";
	import { mergeClasses } from "./helpers";
	import type { AccordionTriggerProps } from "./types";

	const generatedId = $props.id();

	let {
		disabled = false,
		children,
		ref = $bindable(null),
		class: className,
		id,
		type = "button",
		onclick,
		onkeydown,
		...restProps
	}: AccordionTriggerProps = $props();

	const item = getAccordionItemContext("AccordionTrigger");
	const unregisterId = item.registerTriggerId(() => id ?? generatedId);
	const isDisabled = $derived(item.disabled || disabled);
	const cannotCollapse = $derived(item.collapseDisabled && !isDisabled);

	onDestroy(unregisterId);

	const handleClick = (event: Parameters<NonNullable<AccordionTriggerProps["onclick"]>>[0]) => {
		onclick?.(event);

		if (!event.defaultPrevented) item.toggle(disabled);
	};

	const handleKeydown = (event: Parameters<NonNullable<AccordionTriggerProps["onkeydown"]>>[0]) => {
		onkeydown?.(event);

		if (!event.defaultPrevented) item.navigate(event);
	};
</script>

<button
	{...restProps}
	bind:this={ref}
	id={id ?? generatedId}
	{type}
	disabled={isDisabled}
	class={mergeClasses("accordion-trigger", className)}
	aria-expanded={item.open}
	aria-controls={item.contentId}
	aria-disabled={cannotCollapse || undefined}
	data-accordion-trigger
	data-state={item.open ? "open" : "closed"}
	data-disabled={isDisabled ? "" : undefined}
	onclick={handleClick}
	onkeydown={handleKeydown}
>
	<span class="accordion-trigger__label">{@render children({ open: item.open })}</span>
	<span class="accordion-trigger__icon" aria-hidden="true">
		<ChevronDown size={20} strokeWidth={1.75} />
	</span>
</button>

<style>
	.accordion-trigger {
		display: flex;
		width: 100%;
		min-height: 2.75rem;
		align-items: center;
		justify-content: space-between;
		gap: 1rem;
		border: 0;
		border-radius: var(--radius-control);
		background: transparent;
		padding: 0.75rem 0.25rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 1rem;
		font-weight: 600;
		line-height: 1.4;
		text-align: start;
		cursor: pointer;
		transition:
			background-color 150ms ease,
			color 150ms ease;
	}

	.accordion-trigger:hover:not(:disabled) {
		background: var(--archive-layer);
		color: var(--register-mark);
	}

	.accordion-trigger[data-state="open"] {
		background: color-mix(in srgb, var(--archive-layer) 72%, transparent);
	}

	.accordion-trigger:disabled {
		color: var(--marginal-note);
		cursor: not-allowed;
		opacity: 0.72;
	}

	.accordion-trigger[aria-disabled="true"] {
		cursor: default;
	}

	.accordion-trigger__label {
		min-width: 0;
	}

	.accordion-trigger__icon {
		display: inline-flex;
		flex: none;
		transition: transform 150ms ease;
	}

	.accordion-trigger[data-state="open"] .accordion-trigger__icon {
		transform: rotate(180deg);
	}

	@media (prefers-reduced-motion: reduce) {
		.accordion-trigger,
		.accordion-trigger__icon {
			transition: none;
		}
	}
</style>
