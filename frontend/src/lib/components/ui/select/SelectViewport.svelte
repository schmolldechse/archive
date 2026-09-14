<script lang="ts">
	import { onDestroy } from "svelte";

	import { isPrintableKey } from "./helpers";
	import { getSelectContext } from "./select-context.svelte";
	import type { SelectViewportProps } from "./types";

	let {
		children,
		ref = $bindable(null),
		class: className,
		id,
		"aria-label": ariaLabel,
		"aria-labelledby": ariaLabelledby,
		onkeydown,
		...restProps
	}: SelectViewportProps = $props();

	const select = getSelectContext("SelectViewport");
	const fallbackId = select.listboxId;
	const unregisterViewport = select.registerViewport(() => ref);
	const unregisterId = select.registerListboxId(() => id ?? fallbackId);

	onDestroy(() => {
		unregisterViewport();
		unregisterId();
	});

	const handleKeydown: NonNullable<SelectViewportProps["onkeydown"]> = (event) => {
		onkeydown?.(event);
		if (event.defaultPrevented || select.disabled) return;

		if (event.key === "ArrowDown" || event.key === "ArrowUp") {
			event.preventDefault();
			select.moveHighlight(event.key === "ArrowDown" ? 1 : -1);
			return;
		}

		if (event.key === "Home" || event.key === "End") {
			event.preventDefault();
			select.requestOpenChange(true, event.key === "Home" ? "first" : "last");
			return;
		}

		if (event.key === "Enter" || event.key === " ") {
			event.preventDefault();
			select.selectHighlighted("keyboard");
			return;
		}

		if (event.key === "Escape") {
			event.preventDefault();
			select.requestOpenChange(false, undefined, true);
			return;
		}

		if (event.key === "Tab") {
			queueMicrotask(() => select.requestOpenChange(false));
			return;
		}

		if (isPrintableKey(event) && select.typeahead) {
			event.preventDefault();
			select.handleTypeahead(event.key);
		}
	};
</script>

<div
	{...restProps}
	bind:this={ref}
	id={id ?? fallbackId}
	role="listbox"
	tabindex={-1}
	class={["select-viewport", className]}
	aria-label={ariaLabel}
	aria-labelledby={ariaLabelledby ?? (ariaLabel ? undefined : select.labelId ?? select.triggerId)}
	aria-multiselectable={select.type === "multiple" ? "true" : undefined}
	aria-activedescendant={select.open ? select.highlightedId ?? undefined : undefined}
	data-select-viewport
	data-type={select.type}
	onkeydown={handleKeydown}
>
	{@render children()}
</div>

<style>
	.select-viewport {
		display: grid;
		max-height: min(20rem, calc(100dvh - 2rem));
		overflow: auto;
		overscroll-behavior: contain;
		border-radius: calc(var(--radius-control) - 1px);
	}
</style>
