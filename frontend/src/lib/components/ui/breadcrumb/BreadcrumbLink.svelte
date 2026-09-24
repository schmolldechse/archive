<script lang="ts">
	import type { BreadcrumbLinkProps } from "./types";
	let { children, class: className, ...restProps }: BreadcrumbLinkProps = $props();
</script>

<a {...restProps} class={["breadcrumb-link", className]} data-breadcrumb-link>
	{@render children()}
</a>

<style>
	.breadcrumb-link {
		color: inherit;
		text-decoration: none;
		text-underline-offset: 0.16em;
		transition: color 150ms ease;
	}
	.breadcrumb-link:hover {
		color: var(--reading-ink);
		text-decoration: underline;
	}
	@media (prefers-reduced-motion: reduce) {
		.breadcrumb-link {
			transition: none;
		}
	}
</style>
