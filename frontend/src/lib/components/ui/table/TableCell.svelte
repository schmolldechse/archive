<script lang="ts">
	import type { TableCellProps } from "./types";
	let { children, align = "start", valueKind = "text", class: className, ...restProps }: TableCellProps = $props();
</script>

<td
	{...restProps}
	class={["table-cell px-4 py-3 align-middle", className]}
	data-table-cell
	data-align={align}
	data-value-kind={valueKind}
>
	{@render children()}
</td>

<style>
	.table-cell {
		text-align: start;
		overflow-wrap: anywhere;
	}
	.table-cell[data-align="end"] {
		text-align: end;
		font-variant-numeric: tabular-nums;
	}
	.table-cell[data-value-kind="record"] {
		font-family: var(--font-record);
		font-size: 0.8125rem;
		font-variant-numeric: tabular-nums;
	}
</style>
