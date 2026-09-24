<script lang="ts">
	import ChevronRight from "@lucide/svelte/icons/chevron-right";
	import type { BreadcrumbSeparatorProps } from "./types";
	let { children, class: className, ...restProps }: BreadcrumbSeparatorProps = $props();
</script>

<li {...restProps} class={["breadcrumb-separator", className]} role="presentation" aria-hidden="true" data-breadcrumb-separator>
	{#if children}{@render children()}{:else}<ChevronRight aria-hidden="true" />{/if}
</li>

<style>
	.breadcrumb-separator {
		display: inline-flex;
		align-items: center;
		color: var(--marginal-note);
	}
	.breadcrumb-separator :global(svg) {
		width: 0.875rem;
		height: 0.875rem;
		stroke-width: 1.75;
	}
</style>
