<script lang="ts">
	import { onDestroy } from "svelte";

	import { getDialogContext } from "./dialog-context.svelte";
	import type { DialogDescriptionProps } from "./types";

	const generatedId = $props.id();

	let { children, ref = $bindable(null), class: className, id, ...restProps }: DialogDescriptionProps = $props();

	const dialog = getDialogContext("DialogDescription");
	const unregisterId = dialog.registerDescriptionId(() => id ?? generatedId);

	onDestroy(unregisterId);
</script>

<p {...restProps} bind:this={ref} id={id ?? generatedId} class={["dialog-description", className]} data-dialog-description>
	{@render children()}
</p>

<style>
	.dialog-description {
		margin: 0.75rem 0 0;
		color: var(--marginal-note);
		font-family: var(--font-interface);
		font-size: 1rem;
		line-height: 1.5;
	}
</style>
