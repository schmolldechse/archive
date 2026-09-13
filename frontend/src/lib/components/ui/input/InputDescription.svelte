<script lang="ts">
	import { getInputContext } from "./input-context.svelte";
	import type { InputDescriptionProps } from "./types";

	let { children, ref = $bindable(null), class: className, ...restProps }: InputDescriptionProps = $props();

	const input = getInputContext("InputDescription");
</script>

{#if input.hasDescription}
	<p {...restProps} bind:this={ref} id={input.descriptionId} class={["input-description", className]} data-input-description>
		{@render children()}
	</p>
{/if}

<style>
	.input-description {
		margin: 0;
		color: var(--marginal-note);
		font-family: var(--font-interface);
		font-size: 0.875rem;
		line-height: 1.43;
		overflow-wrap: anywhere;
	}
</style>
