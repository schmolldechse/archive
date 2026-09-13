<script lang="ts">
	import type { HTMLAttributes } from "svelte/elements";

	type SkeletonVariant = "text" | "block" | "circle";
	type SkeletonDimension = string | number;
	type NativeSkeletonProps = Omit<
		HTMLAttributes<HTMLSpanElement>,
		"children" | "role" | "aria-hidden" | "tabindex" | "contenteditable"
	>;
	const CSS_NUMBER = String.raw`(?:\d+(?:\.\d+)?|\.\d+)`;
	const CSS_LENGTH_UNIT =
		"(?:px|cm|mm|q|in|pc|pt|em|rem|ex|rex|cap|rcap|ch|rch|ic|ric|lh|rlh|vw|vh|vi|vb|vmin|vmax|svw|svh|svi|svb|svmin|svmax|lvw|lvh|lvi|lvb|lvmin|lvmax|dvw|dvh|dvi|dvb|dvmin|dvmax|cqw|cqh|cqi|cqb|cqmin|cqmax|%)";
	const CSS_LENGTH = String.raw`(?:0|${CSS_NUMBER}${CSS_LENGTH_UNIT})`;
	const SIMPLE_DIMENSION_PATTERN = new RegExp(
		String.raw`^(?:${CSS_LENGTH}|auto|min-content|max-content|fit-content|stretch)$`,
		"i"
	);
	const CALC_DIMENSION_PATTERN = new RegExp(String.raw`^calc\(\s*${CSS_LENGTH}(?:\s+[+-]\s+${CSS_LENGTH})+\s*\)$`, "i");
	const MIN_MAX_DIMENSION_PATTERN = new RegExp(String.raw`^(?:min|max)\(\s*${CSS_LENGTH}(?:\s*,\s*${CSS_LENGTH})+\s*\)$`, "i");
	const CLAMP_DIMENSION_PATTERN = new RegExp(
		String.raw`^clamp\(\s*${CSS_LENGTH}\s*,\s*${CSS_LENGTH}\s*,\s*${CSS_LENGTH}\s*\)$`,
		"i"
	);
	const FIT_CONTENT_DIMENSION_PATTERN = new RegExp(String.raw`^fit-content\(\s*${CSS_LENGTH}\s*\)$`, "i");

	interface SkeletonProps extends NativeSkeletonProps {
		variant?: SkeletonVariant;
		width?: SkeletonDimension;
		height?: SkeletonDimension;
		ref?: HTMLSpanElement | null;
	}

	let { variant = "block", width, height, ref = $bindable(null), class: className, ...restProps }: SkeletonProps = $props();

	function normalizeDimension(value: SkeletonDimension | undefined): string | undefined {
		if (typeof value === "number") {
			return Number.isFinite(value) && value >= 0 ? `${value}px` : undefined;
		}

		const normalizedValue = value?.trim();
		if (!normalizedValue) {
			return undefined;
		}

		const isSupportedDimension = [
			SIMPLE_DIMENSION_PATTERN,
			CALC_DIMENSION_PATTERN,
			MIN_MAX_DIMENSION_PATTERN,
			CLAMP_DIMENSION_PATTERN,
			FIT_CONTENT_DIMENSION_PATTERN
		].some((pattern) => pattern.test(normalizedValue));

		return isSupportedDimension ? normalizedValue : undefined;
	}

	const normalizedWidth = $derived(normalizeDimension(width));
	const normalizedHeight = $derived(normalizeDimension(height));
	const circleSize = $derived(variant === "circle" ? (normalizedWidth ?? normalizedHeight) : undefined);
	const resolvedWidth = $derived(circleSize ?? normalizedWidth);
	const resolvedHeight = $derived(variant === "circle" ? undefined : normalizedHeight);
</script>

<span
	{...restProps}
	bind:this={ref}
	aria-hidden="true"
	class={["skeleton", className]}
	data-component="skeleton"
	data-variant={variant}
	style:--skeleton-width={resolvedWidth}
	style:--skeleton-height={resolvedHeight}
></span>

<style>
	:where(.skeleton) {
		display: block;
		flex: none;
		box-sizing: border-box;
		inline-size: var(--skeleton-width);
		border: 1px solid color-mix(in srgb, var(--border-trace) 32%, transparent);
		background: color-mix(in srgb, var(--archive-layer) 88%, var(--reading-room));
		pointer-events: none;
	}

	:where(.skeleton[data-variant="text"]) {
		block-size: var(--skeleton-height, 0.875rem);
		border-radius: var(--radius-mark);
	}

	:where(.skeleton[data-variant="block"]) {
		block-size: var(--skeleton-height, 4rem);
		border-radius: var(--radius-control);
	}

	:where(.skeleton[data-variant="circle"]) {
		inline-size: var(--skeleton-width, 2.5rem);
		border-radius: 999px;
	}

	.skeleton[data-variant="circle"] {
		aspect-ratio: 1;
		block-size: auto;
	}
</style>
