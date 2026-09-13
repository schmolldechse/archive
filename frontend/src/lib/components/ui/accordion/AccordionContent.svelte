<script lang="ts">
	import { onDestroy } from "svelte";

	import { getAccordionItemContext } from "./accordion-context.svelte";
	import { mergeClasses } from "./helpers";
	import type { AccordionContentProps } from "./types";
	import type { Attachment } from "svelte/attachments";

	const generatedId = $props.id();

	let {
		forceMount = false,
		hiddenUntilFound = false,
		region = false,
		children,
		ref = $bindable(null),
		class: className,
		id,
		...restProps
	}: AccordionContentProps = $props();

	const item = getAccordionItemContext("AccordionContent");
	const unregisterId = item.registerContentId(() => id ?? generatedId);
	const beforeMatchAttachment: Attachment<HTMLDivElement> = (element: HTMLDivElement) => {
		const handleBeforeMatch = (): void => item.openFromSearch();

		element.addEventListener("beforematch", handleBeforeMatch);
		return () => element.removeEventListener("beforeinput", handleBeforeMatch);
	};
	const mounted = $derived(item.open || forceMount || hiddenUntilFound);
	const hidden = $derived(item.open ? undefined : hiddenUntilFound ? "until-found" : true);

	onDestroy(unregisterId);
</script>

{#if mounted}
	<div
		{...restProps}
		bind:this={ref}
		id={id ?? generatedId}
		class={mergeClasses("accordion-content", className)}
		{hidden}
		role={region ? "region" : undefined}
		aria-labelledby={region ? item.triggerId : undefined}
		data-accordion-content
		data-state={item.open ? "open" : "closed"}
		data-mounted
		{@attach hiddenUntilFound && beforeMatchAttachment}
	>
		<div class="accordion-content__inner">
			{@render children()}
		</div>
	</div>
{/if}

<style>
	.accordion-content {
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 1rem;
		line-height: 1.6;
	}

	.accordion-content__inner {
		padding: 0.75rem 0.25rem 1.5rem;
	}

	.accordion-content__inner :global(:first-child) {
		margin-block-start: 0;
	}

	.accordion-content__inner :global(:last-child) {
		margin-block-end: 0;
	}
</style>
