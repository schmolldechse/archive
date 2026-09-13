<script lang="ts">
	import { mergeClasses } from "./helpers";
	import { getTabsContext, setTabsListContext } from "./tabs-context.svelte";
	import type { TabsListProps } from "./types";

	let {
		children,
		ref = $bindable(null),
		class: className,
		"aria-label": ariaLabel,
		"aria-labelledby": ariaLabelledby,
		...restProps
	}: TabsListProps = $props();

	const tabs = getTabsContext("TabsList");
	setTabsListContext();

	const accessibleName = $derived.by(() => {
		if (!ariaLabel?.trim() && !ariaLabelledby?.trim()) {
			throw new Error("TabsList requires an aria-label or aria-labelledby value.");
		}

		return {
			label: ariaLabel?.trim() || undefined,
			labelledby: ariaLabelledby?.trim() || undefined
		};
	});
</script>

<div
	{...restProps}
	bind:this={ref}
	class={mergeClasses("tabs-list", className)}
	role="tablist"
	aria-label={accessibleName.label}
	aria-labelledby={accessibleName.labelledby}
	aria-orientation={tabs.orientation === "vertical" ? "vertical" : undefined}
	data-tabs-list
	data-orientation={tabs.orientation}
>
	{@render children()}
</div>

<style>
	.tabs-list {
		display: flex;
		max-width: 100%;
		flex-wrap: wrap;
		align-items: stretch;
		gap: 0.25rem;
		border-block-end: 1px solid var(--border-trace);
		font-family: var(--font-interface);
	}

	.tabs-list[data-orientation="vertical"] {
		flex-direction: column;
		flex-wrap: nowrap;
		gap: 0.25rem;
		border-block-end: 0;
		border-inline-start: 1px solid var(--border-trace);
	}
</style>
