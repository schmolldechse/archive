<script lang="ts">
	import { onDestroy } from "svelte";

	import { getSelectContext } from "./select-context.svelte";
	import type { SelectLabelProps } from "./types";

	let {
		children,
		ref = $bindable(null),
		class: className,
		id,
		...restProps
	}: SelectLabelProps = $props();

	const select = getSelectContext("SelectLabel");
	const fallbackId = select.defaultLabelId;
	const unregisterId = select.registerLabelId(() => id ?? fallbackId);

	onDestroy(unregisterId);
</script>

<label
	{...restProps}
	bind:this={ref}
	id={id ?? fallbackId}
	for={select.triggerId}
	class={["select-label", className]}
	data-select-label
	data-disabled={select.disabled ? "" : undefined}
	data-required={select.required ? "" : undefined}
>
	{@render children()}
</label>

<style>
	.select-label {
		width: fit-content;
		max-width: 100%;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 0.9375rem;
		font-weight: 600;
		line-height: 1.4;
		overflow-wrap: anywhere;
	}

	.select-label[data-disabled] {
		color: var(--marginal-note);
	}
</style>
