<script lang="ts">
	import { onDestroy } from "svelte";

	import { getSelectContext } from "./select-context.svelte";
	import type { SelectDescriptionProps } from "./types";

	let {
		children,
		ref = $bindable(null),
		class: className,
		id,
		...restProps
	}: SelectDescriptionProps = $props();

	const select = getSelectContext("SelectDescription");
	const fallbackId = select.defaultDescriptionId;
	const unregisterId = select.registerDescriptionId(() => id ?? fallbackId);

	onDestroy(unregisterId);
</script>

<p
	{...restProps}
	bind:this={ref}
	id={id ?? fallbackId}
	class={["select-description", className]}
	data-select-description
>
	{@render children()}
</p>

<style>
	.select-description {
		margin: 0;
		color: var(--marginal-note);
		font-family: var(--font-interface);
		font-size: 0.875rem;
		line-height: 1.43;
		overflow-wrap: anywhere;
	}
</style>
