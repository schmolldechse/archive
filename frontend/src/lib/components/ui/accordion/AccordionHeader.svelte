<script lang="ts">
	import { getAccordionItemContext } from "./accordion-context.svelte";
	import { mergeClasses } from "./helpers";
	import type { AccordionHeaderProps } from "./types";

	let {
		level,
		children,
		ref = $bindable(null),
		class: className,
		...restProps
	}: AccordionHeaderProps = $props();

	const item = getAccordionItemContext("AccordionHeader");
	const resolvedLevel = $derived(level ?? item.headingLevel);
	const headingTag = $derived(`h${resolvedLevel}` as const);
</script>

<svelte:element
	this={headingTag}
	{...restProps}
	bind:this={ref}
	class={mergeClasses("accordion-header", className)}
	data-accordion-header
	data-heading-level={resolvedLevel}
>
	{@render children()}
</svelte:element>

<style>
	.accordion-header {
		margin: 0;
		font: inherit;
	}
</style>
