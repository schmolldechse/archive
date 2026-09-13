<script lang="ts">
	import Check from "@lucide/svelte/icons/check";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import Info from "@lucide/svelte/icons/info";
	import TriangleAlert from "@lucide/svelte/icons/triangle-alert";
	import type { Snippet } from "svelte";
	import type { HTMLAttributes } from "svelte/elements";

	type BadgeVariant = "neutral" | "info" | "success" | "warning" | "critical";
	type BadgeSize = "compact" | "default";
	type NativeSpanProps = Omit<HTMLAttributes<HTMLSpanElement>, "children">;

	interface BadgeProps extends NativeSpanProps {
		variant?: BadgeVariant;
		size?: BadgeSize;
		children: Snippet;
		icon?: Snippet;
		ref?: HTMLSpanElement | null;
	}

	let {
		variant = "neutral",
		size = "default",
		children,
		icon,
		ref = $bindable(null),
		class: className,
		...restProps
	}: BadgeProps = $props();
</script>

<span
	{...restProps}
	bind:this={ref}
	class={["badge", className]}
	data-component="badge"
	data-variant={variant}
	data-size={size}
>
	{#if icon || variant !== "neutral"}
		<span class="badge__icon" aria-hidden="true">
			{#if icon}
				{@render icon()}
			{:else if variant === "info"}
				<Info />
			{:else if variant === "success"}
				<Check />
			{:else if variant === "warning"}
				<TriangleAlert />
			{:else if variant === "critical"}
				<CircleAlert />
			{/if}
		</span>
	{/if}
	<span class="badge__label">{@render children()}</span>
</span>

<style>
	.badge {
		--badge-accent: var(--reading-ink);

		display: inline-flex;
		max-width: 100%;
		align-items: center;
		gap: 0.375rem;
		box-sizing: border-box;
		border: 1px solid var(--border-trace);
		border-radius: 999px;
		background: color-mix(in srgb, var(--archive-layer) 60%, transparent);
		padding: 0.1875rem 0.625rem;
		color: var(--badge-accent);
		font-family: var(--font-interface);
		font-size: 0.8125rem;
		font-weight: 600;
		font-variant-numeric: tabular-nums;
		line-height: 1.25rem;
		vertical-align: middle;
	}

	.badge[data-size="compact"] {
		gap: 0.25rem;
		padding: 0.0625rem 0.5rem;
		font-size: 0.75rem;
		line-height: 1rem;
	}

	.badge[data-variant="info"] {
		--badge-accent: var(--register-mark);
	}

	.badge[data-variant="success"] {
		--badge-accent: var(--preservation-green);
	}

	.badge[data-variant="warning"] {
		--badge-accent: var(--warning-ochre);
	}

	.badge[data-variant="critical"] {
		--badge-accent: var(--time-marker);
	}

	.badge:not([data-variant="neutral"]) {
		border-color: color-mix(in srgb, var(--badge-accent) 48%, transparent);
		background: color-mix(in srgb, var(--badge-accent) 9%, transparent);
	}

	.badge__icon {
		display: inline-flex;
		width: 1rem;
		height: 1rem;
		flex: none;
		align-items: center;
		justify-content: center;
	}

	.badge[data-size="compact"] .badge__icon {
		width: 0.875rem;
		height: 0.875rem;
	}

	.badge__icon :global(svg) {
		width: 100%;
		height: 100%;
		stroke-width: 1.75;
	}

	.badge__label {
		min-width: 0;
		overflow-wrap: anywhere;
	}
</style>
