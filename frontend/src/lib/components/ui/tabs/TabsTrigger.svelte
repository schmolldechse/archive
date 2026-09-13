<script lang="ts">
	import { onDestroy } from "svelte";

	import { mergeClasses } from "./helpers";
	import { getTabsContext, getTabsListContext } from "./tabs-context.svelte";
	import type { TabsTriggerProps } from "./types";

	let {
		value,
		disabled = false,
		children,
		ref = $bindable(null),
		class: className,
		onclick,
		onfocus,
		onblur,
		onkeydown,
		...restProps
	}: TabsTriggerProps = $props();

	const tabs = getTabsContext("TabsTrigger");
	getTabsListContext("TabsTrigger");

	const resolvedId = $derived(tabs.getDefaultTriggerId(value));
	const selected = $derived(tabs.isSelected(value));
	const isDisabled = $derived(tabs.isTriggerDisabled(value, disabled));
	const tabIndex = $derived(tabs.isTabStop(value, disabled) ? 0 : -1);

	const unregister = tabs.registerTrigger({
		get disabled() {
			return disabled;
		},
		get element() {
			return ref;
		},
		get value() {
			return value;
		}
	});

	onDestroy(unregister);

	const handleClick = (event: Parameters<NonNullable<TabsTriggerProps["onclick"]>>[0]): void => {
		onclick?.(event);

		if (!event.defaultPrevented) tabs.activate(value, disabled);
	};

	const handleFocus = (event: Parameters<NonNullable<TabsTriggerProps["onfocus"]>>[0]): void => {
		onfocus?.(event);

		if (!event.defaultPrevented) tabs.focusTrigger(value, disabled);
	};

	const handleBlur = (event: Parameters<NonNullable<TabsTriggerProps["onblur"]>>[0]): void => {
		onblur?.(event);

		if (!event.defaultPrevented) tabs.blurTrigger(event.relatedTarget);
	};

	const handleKeydown = (event: Parameters<NonNullable<TabsTriggerProps["onkeydown"]>>[0]): void => {
		onkeydown?.(event);

		if (event.defaultPrevented) return;

		if (tabs.activationMode === "manual" && (event.key === "Enter" || event.key === " ")) {
			event.preventDefault();
			tabs.activate(value, disabled);
			return;
		}

		tabs.navigate(event, value);
	};
</script>

<button
	{...restProps}
	bind:this={ref}
	id={resolvedId}
	type="button"
	disabled={isDisabled}
	class={mergeClasses("tabs-trigger", className)}
	role="tab"
	aria-selected={selected}
	aria-controls={tabs.getPanelId(value)}
	tabindex={tabIndex}
	data-tabs-trigger
	data-value={value}
	data-state={selected ? "active" : "inactive"}
	data-disabled={isDisabled ? "" : undefined}
	onclick={handleClick}
	onfocus={handleFocus}
	onblur={handleBlur}
	onkeydown={handleKeydown}
>
	{@render children({ selected })}
</button>

<style>
	.tabs-trigger {
		display: inline-flex;
		min-width: min(100%, 5.5rem);
		min-height: 2.75rem;
		align-items: center;
		justify-content: flex-start;
		box-sizing: border-box;
		margin-block-end: -1px;
		border: 0;
		border-block-end: 3px solid transparent;
		border-radius: var(--radius-control) var(--radius-control) 0 0;
		background: transparent;
		padding: 0.75rem 1rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 0.9375rem;
		font-weight: 400;
		line-height: 1.25rem;
		text-align: start;
		overflow-wrap: anywhere;
		cursor: pointer;
	}

	.tabs-trigger:hover:not(:disabled) {
		background: var(--archive-layer);
		color: var(--register-mark);
	}

	.tabs-trigger[data-state="active"] {
		border-block-end-color: var(--register-mark);
		color: var(--register-mark);
		font-weight: 600;
	}

	.tabs-trigger:disabled {
		color: var(--marginal-note);
		cursor: not-allowed;
		opacity: 0.72;
	}

	:global([data-tabs-list][data-orientation="vertical"]) .tabs-trigger {
		width: calc(100% + 1px);
		margin-block-end: 0;
		margin-inline-start: -1px;
		border-block-end: 0;
		border-inline-start: 3px solid transparent;
		border-radius: 0 var(--radius-control) var(--radius-control) 0;
	}

	:global([data-tabs-list][data-orientation="vertical"]) .tabs-trigger[data-state="active"] {
		border-inline-start-color: var(--register-mark);
	}
</style>
