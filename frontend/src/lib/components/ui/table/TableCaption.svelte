<script lang="ts">
	import type { TableCaptionProps } from "./types";
	let { children, class: className, ...restProps }: TableCaptionProps = $props();
</script>

<caption {...restProps} class={["text-reading-ink pb-4 text-start font-sans font-semibold", className]} data-table-caption>
	{@render children()}
</caption>
