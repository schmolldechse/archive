<script lang="ts">
	import type { TableHeadProps } from "./types";
	let { children, scope = "col", align = "start", class: className, ...restProps }: TableHeadProps = $props();
</script>

<th
	{...restProps}
	{scope}
	class={["table-head px-4 py-3 align-top font-semibold", className]}
	data-table-head
	data-align={align}
>
	{@render children()}
</th>

<style>
	.table-head {
		text-align: start;
		overflow-wrap: anywhere;
	}
	.table-head[data-align="end"] {
		text-align: end;
		font-variant-numeric: tabular-nums;
	}
</style>
