<script lang="ts">
	import type { HTMLAttributes } from "svelte/elements";

	type SeparatorOrientation = "horizontal" | "vertical";
	type NativeSeparatorProps = Omit<
		HTMLAttributes<HTMLDivElement>,
		"children" | "role" | "aria-hidden" | "aria-orientation" | "tabindex"
	>;

	interface SeparatorProps extends NativeSeparatorProps {
		orientation?: SeparatorOrientation;
		decorative?: boolean;
		ref?: HTMLDivElement | null;
	}

	let {
		orientation = "horizontal",
		decorative = true,
		ref = $bindable(null),
		class: className,
		...restProps
	}: SeparatorProps = $props();
</script>

<div
	{...restProps}
	bind:this={ref}
	role={decorative ? "none" : "separator"}
	aria-hidden={decorative ? "true" : undefined}
	aria-orientation={!decorative && orientation === "vertical" ? "vertical" : undefined}
	class={["separator", className]}
	data-component="separator"
	data-orientation={orientation}
	data-decorative={decorative ? "" : undefined}
></div>

<style>
	.separator {
		box-sizing: border-box;
		flex: none;
		margin: 0;
		border: 0;
		background: var(--border-trace);
	}

	.separator[data-orientation="horizontal"] {
		inline-size: 100%;
		block-size: 1px;
	}

	.separator[data-orientation="vertical"] {
		inline-size: 1px;
		block-size: 100%;
	}
</style>
