<script lang="ts">
	import ChevronRight from "@lucide/svelte/icons/chevron-right";

	import { getCalendarContext } from "./calendar-context.svelte";
	import type { CalendarNextButtonProps } from "./types";

	let { children, ref = $bindable(null), class: className, onclick, ...restProps }: CalendarNextButtonProps = $props();

	const calendar = getCalendarContext("CalendarNextButton");
	const unavailable = $derived(!calendar.isNavigationAvailable(1));
	const accessibleLabel = $derived(calendar.getNavigationLabel(1));

	const handleClick = (event: Parameters<NonNullable<CalendarNextButtonProps["onclick"]>>[0]): void => {
		onclick?.(event);
		if (!event.defaultPrevented) calendar.navigateMonth(1);
	};
</script>

<button
	{...restProps}
	bind:this={ref}
	type="button"
	disabled={unavailable}
	class={["calendar-navigation", className]}
	aria-label={accessibleLabel}
	data-calendar-next
	data-disabled={unavailable ? "" : undefined}
	onclick={handleClick}
>
	{#if children}
		{@render children()}
	{:else}
		<ChevronRight aria-hidden="true" />
	{/if}
</button>

<style>
	.calendar-navigation {
		display: inline-flex;
		width: 2.75rem;
		height: 2.75rem;
		align-items: center;
		justify-content: center;
		appearance: none;
		border: 1px solid transparent;
		border-radius: var(--radius-control);
		background: transparent;
		color: var(--register-mark);
		cursor: pointer;
		transition:
			background-color 150ms ease,
			border-color 150ms ease,
			color 150ms ease;
	}

	.calendar-navigation:hover:not(:disabled) {
		border-color: var(--border-trace);
		background: var(--archive-layer);
	}

	.calendar-navigation:disabled {
		color: var(--marginal-note);
		cursor: not-allowed;
		opacity: 0.58;
	}

	.calendar-navigation :global(svg) {
		width: 1.25rem;
		height: 1.25rem;
		stroke-width: 1.75;
	}

	@media (prefers-reduced-motion: reduce) {
		.calendar-navigation {
			transition: none;
		}
	}
</style>
