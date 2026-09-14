<script lang="ts">
	import type { SelectSeparatorProps } from "./types";

	let {
		ref = $bindable(null),
		class: className,
		...restProps
	}: SelectSeparatorProps = $props();
</script>

<div
	{...restProps}
	bind:this={ref}
	role="separator"
	aria-orientation="horizontal"
	class={["select-separator", className]}
	data-select-separator
></div>

<style>
	.select-separator {
		height: 1px;
		margin: 0.25rem;
		background: var(--border-trace);
	}
</style>
