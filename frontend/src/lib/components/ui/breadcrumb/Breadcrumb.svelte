<script lang="ts">
	import type { BreadcrumbProps } from "./types";
	let { children, class: className, "aria-label": label = "Breadcrumb", ...restProps }: BreadcrumbProps = $props();
</script>

<nav {...restProps} aria-label={label} class={["breadcrumb", className]} data-component="breadcrumb">
	{@render children()}
</nav>

<style>
	.breadcrumb {
		color: var(--marginal-note);
		font-family: var(--font-interface);
		font-size: 0.875rem;
		line-height: 1.5;
	}
</style>
