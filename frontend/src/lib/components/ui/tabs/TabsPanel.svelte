<script lang="ts">
	import { onDestroy } from "svelte";
	import type { Attachment } from "svelte/attachments";

	import { isFirstMeaningfulContentTabbable, mergeClasses } from "./helpers";
	import { getTabsContext } from "./tabs-context.svelte";
	import type { TabsPanelProps } from "./types";

	let { value, forceMount = false, children, ref = $bindable(null), class: className, ...restProps }: TabsPanelProps = $props();

	const tabs = getTabsContext("TabsPanel");
	const resolvedId = $derived(tabs.getDefaultPanelId(value));
	const selected = $derived(tabs.isSelected(value));
	const mounted = $derived(selected || forceMount);
	let firstContentIsTabbable = $state(false);
	const tabIndex = $derived(selected && !firstContentIsTabbable ? 0 : undefined);

	const unregister = tabs.registerPanel({
		get value() {
			return value;
		}
	});

	onDestroy(unregister);

	const observeFocusableContent: Attachment<HTMLDivElement> = (element) => {
		const updateFocusableContent = (): void => {
			firstContentIsTabbable = isFirstMeaningfulContentTabbable(element);
		};

		updateFocusableContent();

		const observer = new MutationObserver(updateFocusableContent);
		observer.observe(element, {
			attributes: true,
			attributeFilter: [
				"aria-hidden",
				"class",
				"contenteditable",
				"disabled",
				"hidden",
				"href",
				"inert",
				"style",
				"tabindex",
				"type"
			],
			childList: true,
			subtree: true
		});
		window.addEventListener("resize", updateFocusableContent);

		return () => {
			observer.disconnect();
			window.removeEventListener("resize", updateFocusableContent);
		};
	};
</script>

{#if mounted}
	<div
		{...restProps}
		bind:this={ref}
		id={resolvedId}
		class={mergeClasses("tabs-panel", className)}
		role="tabpanel"
		aria-labelledby={tabs.getTriggerId(value)}
		hidden={selected ? undefined : true}
		tabindex={tabIndex}
		data-tabs-panel
		data-value={value}
		data-state={selected ? "active" : "inactive"}
		{@attach observeFocusableContent}
	>
		{@render children()}
	</div>
{/if}

<style>
	.tabs-panel {
		min-width: 0;
		padding-block-start: 1.5rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 1rem;
		line-height: 1.6;
	}

	.tabs-panel > :global(:first-child) {
		margin-block-start: 0;
	}

	.tabs-panel > :global(:last-child) {
		margin-block-end: 0;
	}
</style>
