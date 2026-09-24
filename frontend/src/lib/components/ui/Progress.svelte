<script lang="ts">
	import { cubicInOut } from "svelte/easing";
	import { prefersReducedMotion, Tween } from "svelte/motion";
	import type { HTMLAttributes } from "svelte/elements";

	type NativeProgressProps = Omit<
		HTMLAttributes<HTMLDivElement>,
		"children" | "role" | "tabindex" | "aria-label" | "aria-labelledby" | "aria-valuemin" | "aria-valuemax" | "aria-valuenow"
	>;
	type ProgressProps = NativeProgressProps & {
		label: string;
		value?: number | null;
		min?: number;
		max?: number;
		animationDuration?: number;
	};

	let {
		label,
		value = 0,
		min = 0,
		max = 100,
		animationDuration = 1000,
		"aria-valuetext": ariaValueText,
		class: className,
		...restProps
	}: ProgressProps = $props();

	const bounds = $derived.by(() => {
		if (!Number.isFinite(min) || !Number.isFinite(max) || max <= min) {
			throw new RangeError("Progress requires finite min and max, with max greater than min.");
		}

		return { min, max };
	});
	const normalizedValue = $derived(
		value === null || !Number.isFinite(value) ? null : Math.min(bounds.max, Math.max(bounds.min, value))
	);
	const motionDuration = $derived.by(() => {
		if (!Number.isFinite(animationDuration) || animationDuration < 0) {
			throw new RangeError("Progress requires a finite, non-negative animationDuration in milliseconds.");
		}

		return animationDuration;
	});
	const animatedValue = Tween.of(() => normalizedValue ?? bounds.min, {
		duration: (from, to) => (from === to || normalizedValue === null || prefersReducedMotion.current ? 0 : motionDuration),
		easing: cubicInOut
	});
	const visibleValue = $derived(
		normalizedValue === null ? null : Math.min(bounds.max, Math.max(bounds.min, animatedValue.current))
	);
	const percentage = $derived(visibleValue === null ? null : ((visibleValue - bounds.min) / (bounds.max - bounds.min)) * 100);
</script>

<div
	{...restProps}
	role="progressbar"
	aria-label={label}
	aria-valuemin={bounds.min}
	aria-valuemax={bounds.max}
	aria-valuenow={visibleValue ?? undefined}
	aria-valuetext={visibleValue === normalizedValue ? ariaValueText : undefined}
	class={["progress", className]}
	data-component="progress"
	data-state={normalizedValue === null ? "indeterminate" : "determinate"}
	data-min={bounds.min}
	data-max={bounds.max}
	data-value={visibleValue ?? undefined}
	data-target-value={normalizedValue ?? undefined}
	data-complete={visibleValue === bounds.max ? "" : undefined}
	data-animation-duration={motionDuration}
	style:--progress-percentage={percentage === null ? undefined : `${percentage}%`}
>
	<div class="progress__heading" aria-hidden="true">
		<span class="progress__label">{label}</span>
		{#if percentage !== null}<span class="progress__value">{Math.round(percentage)}%</span>{/if}
	</div>
	<div class="progress__track" aria-hidden="true">
		{#if normalizedValue === null}
			<span class="progress__unknown"><span></span><span></span><span></span></span>
		{:else}
			<span class="progress__fill"></span>
		{/if}
	</div>
</div>

<style>
	.progress {
		display: grid;
		box-sizing: border-box;
		inline-size: 100%;
		min-inline-size: 0;
		gap: 0.5rem;
		container-type: inline-size;
	}

	.progress__heading {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto;
		align-items: start;
		gap: 0.25rem 1rem;
		font-family: var(--font-interface);
		font-size: 0.875rem;
		font-weight: 600;
		line-height: 1.43;
	}

	.progress__label {
		min-inline-size: 0;
		overflow-wrap: anywhere;
	}

	.progress__value {
		font-variant-numeric: tabular-nums;
		white-space: nowrap;
	}

	.progress__track {
		display: flex;
		box-sizing: border-box;
		inline-size: 100%;
		block-size: 10px;
		overflow: hidden;
		border: 1px solid color-mix(in srgb, var(--border-trace) 80%, var(--reading-ink));
		border-radius: var(--radius-mark);
		background: var(--archive-layer);
	}

	.progress__fill {
		flex: none;
		inline-size: 100%;
		block-size: 100%;
		background: var(--register-mark);
		transform: translateX(calc(var(--progress-percentage, 0%) - 100%));
	}

	.progress:dir(rtl) .progress__fill {
		transform: translateX(calc(100% - var(--progress-percentage, 0%)));
	}

	.progress__unknown {
		display: grid;
		flex: 1;
		grid-template-columns: repeat(3, minmax(0, 1fr));
		justify-items: center;
		block-size: 100%;
	}

	.progress__unknown > span {
		inline-size: min(1rem, 50%);
		block-size: 100%;
		background: var(--register-mark);
	}

	@container (max-width: 22rem) {
		.progress__heading {
			grid-template-columns: minmax(0, 1fr);
		}
	}
</style>
