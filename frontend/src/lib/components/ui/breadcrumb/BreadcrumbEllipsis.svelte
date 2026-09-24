<script lang="ts">
	import Ellipsis from "@lucide/svelte/icons/ellipsis";
	import type { BreadcrumbEllipsisProps } from "./types";
	let { label = "More pages", class: className, ...restProps }: BreadcrumbEllipsisProps = $props();
</script>

<span {...restProps} class={["breadcrumb-ellipsis", className]} data-breadcrumb-ellipsis>
	<Ellipsis aria-hidden="true" />
	<span class="sr-only">{label}</span>
</span>

<style>
	.breadcrumb-ellipsis {
		display: inline-flex;
		align-items: center;
	}
	.breadcrumb-ellipsis :global(svg) {
		width: 1rem;
		height: 1rem;
	}
	.sr-only {
		position: absolute;
		width: 1px;
		height: 1px;
		padding: 0;
		margin: -1px;
		overflow: hidden;
		clip: rect(0, 0, 0, 0);
		white-space: nowrap;
		border: 0;
	}
</style>
