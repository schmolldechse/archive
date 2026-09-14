<script lang="ts">
	import { onDestroy } from "svelte";

	import { getSelectGroupContext } from "./select-context.svelte";
	import type { SelectGroupLabelProps } from "./types";

	let {
		children,
		ref = $bindable(null),
		class: className,
		id,
		...restProps
	}: SelectGroupLabelProps = $props();

	const group = getSelectGroupContext("SelectGroupLabel");
	const fallbackId = group.defaultLabelId;
	const unregisterId = group.registerLabelId(() => id ?? fallbackId);

	onDestroy(unregisterId);
</script>

<div
	{...restProps}
	bind:this={ref}
	id={id ?? fallbackId}
	class={["select-group-label", className]}
	data-select-group-label
>
	{@render children()}
</div>

<style>
	.select-group-label {
		padding: 0.625rem 0.75rem 0.375rem;
		color: var(--marginal-note);
		font-family: var(--font-interface);
		font-size: 0.75rem;
		font-weight: 600;
		line-height: 1.33;
		letter-spacing: 0.08em;
		text-transform: uppercase;
		overflow-wrap: anywhere;
	}
</style>
