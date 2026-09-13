<script lang="ts">
	import type { DropdownMenuSeparatorProps } from "./types";

	let {
		ref = $bindable(null),
		class: className,
		...restProps
	}: DropdownMenuSeparatorProps = $props();
</script>

<div
	{...restProps}
	bind:this={ref}
	role="separator"
	class={["dropdown-menu-separator", className]}
	data-dropdown-menu-separator
></div>

<style>
	.dropdown-menu-separator {
		height: 1px;
		margin: 0.25rem;
		background: var(--border-trace);
	}
</style>
