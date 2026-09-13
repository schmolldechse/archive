<script lang="ts">
	import { getDialogContext } from "./dialog-context.svelte";
	import type { DialogActionsProps } from "./types";

	let { children, ref = $bindable(null), class: className, ...restProps }: DialogActionsProps = $props();

	getDialogContext("DialogActions");
</script>

<div {...restProps} bind:this={ref} class={["dialog-actions", className]} data-dialog-actions>
	{@render children()}
</div>

<style>
	.dialog-actions {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		justify-content: flex-end;
		gap: 0.75rem;
		margin-block-start: 1.5rem;
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
	}
</style>
