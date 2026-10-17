<script lang="ts">
	import type { TableViewportProps } from "./types";
	let { children, class: className, role = "region", tabindex = 0, ...restProps }: TableViewportProps = $props();
</script>

<!-- svelte-ignore a11y_no_noninteractive_tabindex (A named scroll region needs keyboard focus for scrolling.) -->
<div {...restProps} {role} {tabindex} class={["table-viewport", className]} data-table-viewport>
	{@render children()}
</div>

<style>
	/* Padding leaves room for the full focus outline of controls at scroll edges. */
	.table-viewport {
		position: relative;
		min-inline-size: 0;
		max-inline-size: 100%;
		overflow-x: auto;
		padding: 0.5rem;
	}
</style>
