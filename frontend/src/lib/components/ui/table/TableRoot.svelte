<script lang="ts">
	import type { TableRootProps } from "./types";
	let { children, class: className, ...restProps }: TableRootProps = $props();
</script>

<table {...restProps} class={["table-root text-reading-ink w-full font-sans", className]} data-component="table">
	{@render children()}
</table>

<style>
	.table-root {
		border-collapse: collapse;
		border-spacing: 0;
		font-size: 1rem;
		line-height: 1.5;
	}
</style>
