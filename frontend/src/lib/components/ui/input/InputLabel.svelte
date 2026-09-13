<script lang="ts">
	import { getInputContext } from "./input-context.svelte";
	import type { InputLabelProps } from "./types";

	let { children, ref = $bindable(null), class: className, ...restProps }: InputLabelProps = $props();

	const input = getInputContext("InputLabel");
</script>

<label
	{...restProps}
	bind:this={ref}
	for={input.controlId}
	class={["input-label", className]}
	data-input-label
	data-disabled={input.disabled ? "" : undefined}
	data-required={input.required ? "" : undefined}
>
	{@render children()}
</label>

<style>
	.input-label {
		width: fit-content;
		max-width: 100%;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 0.9375rem;
		font-weight: 600;
		line-height: 1.4;
		overflow-wrap: anywhere;
	}

	.input-label[data-disabled] {
		color: var(--marginal-note);
	}
</style>
