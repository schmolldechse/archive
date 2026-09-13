<script lang="ts">
	import { onDestroy } from "svelte";

	import { getDialogContext } from "./dialog-context.svelte";
	import type { DialogTitleProps } from "./types";

	const generatedId = $props.id();

	let { level = 2, children, ref = $bindable(null), class: className, id, tabindex, ...restProps }: DialogTitleProps = $props();

	const dialog = getDialogContext("DialogTitle");
	const unregisterId = dialog.registerTitleId(() => id ?? generatedId);
	const headingTag = $derived(`h${level}` as const);

	onDestroy(unregisterId);
</script>

<svelte:element
	this={headingTag}
	{...restProps}
	bind:this={ref}
	id={id ?? generatedId}
	class={["dialog-title", className]}
	tabindex={tabindex ?? -1}
	data-dialog-title
	data-heading-level={level}
>
	{@render children()}
</svelte:element>

<style>
	.dialog-title {
		margin: 0;
		color: var(--reading-ink);
		font-family: var(--font-editorial);
		font-size: clamp(1.75rem, 5vw, 2rem);
		font-weight: 500;
		letter-spacing: -0.015em;
		line-height: 1.18;
	}
</style>
