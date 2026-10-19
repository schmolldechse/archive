<script lang="ts">
	import type { TableSectionProps } from "./types";
	let { children, class: className, ...restProps }: TableSectionProps = $props();
</script>

<tbody {...restProps} class={className} data-table-body>{@render children()}</tbody>
