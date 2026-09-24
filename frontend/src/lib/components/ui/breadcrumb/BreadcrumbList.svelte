<script lang="ts">
	import type { BreadcrumbListProps } from "./types";
	let { children, class: className, ...restProps }: BreadcrumbListProps = $props();
</script>

<ol {...restProps} class={["breadcrumb-list", className]} data-breadcrumb-list>
	{@render children()}
</ol>

<style>
	.breadcrumb-list {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 0.35rem 0.5rem;
		margin: 0;
		padding: 0;
		list-style: none;
	}
</style>
