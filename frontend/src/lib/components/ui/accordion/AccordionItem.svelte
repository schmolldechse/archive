<script lang="ts">
	import { onDestroy } from "svelte";

	import { getAccordionRootContext, setAccordionItemContext } from "./accordion-context.svelte";
	import { mergeClasses } from "./helpers";
	import type { AccordionItemProps } from "./types";

	let {
		value,
		disabled = false,
		children,
		ref = $bindable(null),
		class: className,
		...restProps
	}: AccordionItemProps = $props();

	const root = getAccordionRootContext();
	const generatedId = $props.id();
	const defaultTriggerId = `${generatedId}-trigger`;
	const defaultContentId = `${generatedId}-content`;
	const getDefaultTriggerId = (): string => defaultTriggerId;
	const getDefaultContentId = (): string => defaultContentId;
	let getTriggerId = $state<() => string>(getDefaultTriggerId);
	let getContentId = $state<() => string>(getDefaultContentId);

	const unregisterItem = root.registerItem({
		get value() {
			return value;
		}
	});

	onDestroy(unregisterItem);

	const registerTriggerId = (getId: () => string) => {
		getTriggerId = getId;
		return () => {
			if (getTriggerId === getId) getTriggerId = getDefaultTriggerId;
		};
	};

	const registerContentId = (getId: () => string) => {
		getContentId = getId;
		return () => {
			if (getContentId === getId) getContentId = getDefaultContentId;
		};
	};

	setAccordionItemContext({
		get collapseDisabled() {
			return root.isCollapseDisabled(value);
		},
		get contentId() {
			return getContentId();
		},
		get disabled() {
			return root.disabled || disabled;
		},
		get headingLevel() {
			return root.headingLevel;
		},
		get open() {
			return root.isOpen(value);
		},
		get triggerId() {
			return getTriggerId();
		},
		get value() {
			return value;
		},
		navigate: root.navigate,
		openFromSearch() {
			root.openFromSearch(value);
		},
		registerContentId,
		registerTriggerId,
		toggle(triggerDisabled: boolean) {
			root.toggle(value, disabled || triggerDisabled);
		}
	});
</script>

<div
	{...restProps}
	bind:this={ref}
	class={mergeClasses("accordion-item", className)}
	data-accordion-item
	data-value={value}
	data-state={root.isOpen(value) ? "open" : "closed"}
	data-disabled={root.disabled || disabled ? "" : undefined}
>
	{@render children({ open: root.isOpen(value) })}
</div>

<style>
	.accordion-item {
		border-block-end: 1px solid var(--border-trace);
	}
</style>
