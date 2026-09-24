<script lang="ts">
	import type { BreadcrumbPageProps } from "./types";
	let { children, class: className, ...restProps }: BreadcrumbPageProps = $props();
</script>

<span {...restProps} class={["breadcrumb-page", className]} aria-current="page" data-breadcrumb-page>
	{@render children()}
</span>

<style>
	.breadcrumb-page {
		color: var(--reading-ink);
		font-weight: 600;
		overflow-wrap: anywhere;
	}
</style>
