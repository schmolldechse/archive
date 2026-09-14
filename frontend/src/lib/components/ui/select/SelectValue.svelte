<script lang="ts">
	import { getSelectContext } from "./select-context.svelte";
	import type { SelectValueProps } from "./types";

	let {
		placeholder = "Select an option",
		children,
		ref = $bindable(null),
		class: className,
		...restProps
	}: SelectValueProps = $props();

	const select = getSelectContext("SelectValue");
	const state = $derived(select.valueState());
	const defaultValue = $derived.by(() => {
		if (state.placeholder) return placeholder;
		if (state.labels.length > 0) return state.labels.join(", ");
		if (Array.isArray(state.value)) return state.value.join(", ");
		return state.value ?? placeholder;
	});
</script>

<span
	{...restProps}
	bind:this={ref}
	class={["select-value", className]}
	data-select-value
	data-placeholder={state.placeholder ? "" : undefined}
>
	{#if children}
		{@render children(state)}
	{:else}
		{defaultValue}
	{/if}
</span>

<style>
	.select-value {
		display: block;
		min-width: 0;
		color: var(--reading-ink);
		overflow-wrap: anywhere;
	}

	.select-value[data-placeholder] {
		color: var(--marginal-note);
	}
</style>
