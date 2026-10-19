<script lang="ts">
	import type { TableSectionProps } from "./types";
	let { children, class: className, ...restProps }: TableSectionProps = $props();
</script>

<tfoot {...restProps} class={["border-border-trace border-t font-semibold", className]} data-table-footer>
	{@render children()}
</tfoot>
