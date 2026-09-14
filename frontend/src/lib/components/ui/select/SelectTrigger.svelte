<script lang="ts">
	import ChevronDown from "@lucide/svelte/icons/chevron-down";
	import { onDestroy } from "svelte";

	import { isPrintableKey, joinIds } from "./helpers";
	import { getSelectContext } from "./select-context.svelte";
	import type { SelectTriggerProps } from "./types";

	let {
		children,
		ref = $bindable(null),
		class: className,
		id,
		"aria-label": ariaLabel,
		"aria-labelledby": ariaLabelledby,
		"aria-describedby": ariaDescribedby,
		onclick,
		onkeydown,
		...restProps
	}: SelectTriggerProps = $props();

	const select = getSelectContext("SelectTrigger");
	const fallbackId = select.triggerId;
	const unregisterTrigger = select.registerTrigger(() => ref);
	const unregisterId = select.registerTriggerId(() => id ?? fallbackId);
	const describedBy = $derived(
		joinIds(ariaDescribedby, select.descriptionId, select.invalid ? select.errorId : undefined)
	);
	const labelledBy = $derived(joinIds(select.labelId, ariaLabelledby));

	onDestroy(() => {
		unregisterTrigger();
		unregisterId();
	});

	const handleClick: NonNullable<SelectTriggerProps["onclick"]> = (event) => {
		onclick?.(event);
		if (event.defaultPrevented || select.disabled) return;

		select.requestOpenChange(!select.open, "selected-or-first", false);
	};

	const handleKeydown: NonNullable<SelectTriggerProps["onkeydown"]> = (event) => {
		onkeydown?.(event);
		if (event.defaultPrevented || select.disabled) return;

		if (event.key === "Enter" || event.key === " ") {
			event.preventDefault();
			select.requestOpenChange(true, "selected-or-first");
			return;
		}

		if (event.key === "ArrowDown" || event.key === "ArrowUp") {
			event.preventDefault();
			select.requestOpenChange(
				true,
				event.key === "ArrowDown" ? "selected-or-first" : "selected-or-last"
			);
			return;
		}

		if (isPrintableKey(event) && select.typeahead) {
			event.preventDefault();
			select.requestOpenChange(true, "selected-or-first");
			select.handleTypeahead(event.key);
		}
	};
</script>

<!-- svelte-ignore a11y_role_supports_aria_props_implicit (The Select specification requires field validation state on the native trigger.) -->
<button
	{...restProps}
	bind:this={ref}
	id={id ?? fallbackId}
	type="button"
	disabled={select.disabled}
	class={["select-trigger", className]}
	aria-label={ariaLabel}
	aria-labelledby={labelledBy}
	aria-haspopup="listbox"
	aria-expanded={select.open}
	aria-controls={select.listboxId}
	aria-describedby={describedBy}
	aria-required={select.required ? "true" : undefined}
	aria-invalid={select.invalid ? "true" : undefined}
	data-select-trigger
	data-state={select.open ? "open" : "closed"}
	data-disabled={select.disabled ? "" : undefined}
	data-invalid={select.invalid ? "" : undefined}
	data-placeholder={select.empty ? "" : undefined}
	onclick={handleClick}
	onkeydown={handleKeydown}
>
	<span class="select-trigger__content">{@render children()}</span>
	<span class="select-trigger__icon" aria-hidden="true"><ChevronDown size={20} strokeWidth={1.75} /></span>
</button>

<style>
	.select-trigger {
		display: flex;
		width: 100%;
		min-width: 0;
		min-height: 2.75rem;
		align-items: center;
		justify-content: space-between;
		gap: 0.75rem;
		box-sizing: border-box;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		padding: 0.625rem 0.75rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 0.9375rem;
		line-height: 1.4;
		text-align: start;
		cursor: pointer;
		transition:
			border-color 150ms ease,
			background-color 150ms ease;
		appearance: none;
	}

	.select-trigger:hover:not(:disabled) {
		border-color: var(--reading-ink);
	}

	.select-trigger[data-state="open"] {
		border-color: var(--register-mark);
		background: var(--reading-room);
	}

	.select-trigger[data-invalid] {
		border-inline-start: 3px solid var(--time-marker);
	}

	.select-trigger:disabled {
		color: var(--marginal-note);
		cursor: not-allowed;
		opacity: 0.68;
	}

	.select-trigger__content {
		min-width: 0;
		flex: 1;
		overflow-wrap: anywhere;
	}

	.select-trigger__icon {
		display: inline-flex;
		width: 1.25rem;
		height: 1.25rem;
		flex: none;
		align-items: center;
		justify-content: center;
		color: var(--marginal-note);
		transition: transform 150ms ease;
	}

	.select-trigger[data-state="open"] .select-trigger__icon {
		color: var(--register-mark);
		transform: rotate(180deg);
	}

	@media (prefers-reduced-motion: reduce) {
		.select-trigger,
		.select-trigger__icon {
			transition: none;
		}
	}
</style>
