<script lang="ts">
	import type { TableSectionProps } from "./types";
	let { children, class: className, ...restProps }: TableSectionProps = $props();
</script>

<thead {...restProps} class={["bg-archive-layer", className]} data-table-header>
	{@render children()}
</thead>
