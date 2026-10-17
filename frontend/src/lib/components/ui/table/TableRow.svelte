<script lang="ts">
	import type { TableRowProps } from "./types";
	let { children, selected = false, class: className, ...restProps }: TableRowProps = $props();
</script>

<tr {...restProps} class={["table-row", className]} data-table-row data-selected={selected ? "" : undefined}>
	{@render children()}
</tr>

<style>
	.table-row {
		border-block-end: 1px solid color-mix(in srgb, var(--border-trace) 45%, transparent);
	}
	.table-row:hover,
	.table-row[data-selected] {
		background: var(--archive-layer);
	}
	/* Reserve the selection rule so toggling selection cannot shift a column. */
	.table-row > :global(:first-child) {
		border-inline-start: 3px solid transparent;
	}
	.table-row[data-selected] > :global(:first-child) {
		border-inline-start-color: var(--register-mark);
	}
</style>
