<script lang="ts">
	import type { BreadcrumbItemProps } from "./types";
	let { children, class: className, ...restProps }: BreadcrumbItemProps = $props();
</script>

<li {...restProps} class={["breadcrumb-item", className]} data-breadcrumb-item>
	{@render children()}
</li>

<style>
	.breadcrumb-item {
		min-width: 0;
		display: inline-flex;
		align-items: center;
	}
</style>
