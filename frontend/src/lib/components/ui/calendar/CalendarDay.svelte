<script lang="ts">
	import { onDestroy } from "svelte";

	import { getCalendarContext } from "./calendar-context.svelte";
	import { toCalendarDateKey } from "./helpers";
	import type { CalendarDayProps } from "./types";

	let {
		date,
		children,
		ref = $bindable(null),
		class: className,
		onclick,
		onfocus,
		onblur,
		onkeydown,
		onpointerenter,
		onpointerleave,
		...restProps
	}: CalendarDayProps = $props();

	const calendar = getCalendarContext("CalendarDay");
	const state = $derived(calendar.getDayState(date));
	const tabIndex = $derived(state.hidden ? -1 : calendar.getTabIndex(date));

	const unregister = calendar.registerDay({
		get date() {
			return date;
		},
		get element() {
			return ref;
		}
	});

	onDestroy(unregister);

	const handleClick = (event: Parameters<NonNullable<CalendarDayProps["onclick"]>>[0]): void => {
		onclick?.(event);
		if (!event.defaultPrevented) calendar.selectDate(date, "pointer");
	};

	const handleFocus = (event: Parameters<NonNullable<CalendarDayProps["onfocus"]>>[0]): void => {
		onfocus?.(event);
		if (!event.defaultPrevented) calendar.setFocusedDate(date);
	};

	const handleBlur = (event: Parameters<NonNullable<CalendarDayProps["onblur"]>>[0]): void => {
		onblur?.(event);
	};

	const handleKeydown = (event: Parameters<NonNullable<CalendarDayProps["onkeydown"]>>[0]): void => {
		onkeydown?.(event);
		if (!event.defaultPrevented) calendar.navigateDay(event, date);
	};

	const handlePointerEnter = (event: Parameters<NonNullable<CalendarDayProps["onpointerenter"]>>[0]): void => {
		onpointerenter?.(event);
		if (!event.defaultPrevented) calendar.setPreviewDate(date);
	};

	const handlePointerLeave = (event: Parameters<NonNullable<CalendarDayProps["onpointerleave"]>>[0]): void => {
		onpointerleave?.(event);
		if (!event.defaultPrevented) calendar.setPreviewDate(null);
	};
</script>

<td
	role="gridcell"
	class="calendar-day-cell"
	aria-selected={state.selected ? "true" : undefined}
	aria-disabled={state.unavailable || state.disabled ? "true" : undefined}
	aria-hidden={state.hidden ? "true" : undefined}
>
	<button
		{...restProps}
		bind:this={ref}
		type="button"
		class={["calendar-day", className]}
		aria-label={state.formattedLabel}
		aria-current={state.today ? "date" : undefined}
		aria-disabled={state.unavailable || state.disabled ? "true" : undefined}
		aria-hidden={state.hidden ? "true" : undefined}
		tabindex={tabIndex}
		data-calendar-day
		data-date={toCalendarDateKey(state.date)}
		data-outside={state.outside ? "" : undefined}
		data-today={state.today ? "" : undefined}
		data-selected={state.selected ? "" : undefined}
		data-unavailable={state.unavailable ? "" : undefined}
		data-disabled={state.disabled ? "" : undefined}
		data-focused={state.focused ? "" : undefined}
		data-range-start={state.rangeStart ? "" : undefined}
		data-range-end={state.rangeEnd ? "" : undefined}
		data-in-range={state.inRange ? "" : undefined}
		data-preview-range={state.inPreviewRange ? "" : undefined}
		data-hidden={state.hidden ? "" : undefined}
		onclick={handleClick}
		onfocus={handleFocus}
		onblur={handleBlur}
		onkeydown={handleKeydown}
		onpointerenter={handlePointerEnter}
		onpointerleave={handlePointerLeave}
	>
		{#if children}
			{@render children(state)}
		{:else}
			<span>{state.dayNumber}</span>
		{/if}
	</button>
</td>

<style>
	.calendar-day-cell {
		width: calc(100% / 7);
		padding: 0;
		border-block-start: 1px solid color-mix(in srgb, var(--border-trace) 52%, transparent);
		text-align: center;
	}

	.calendar-day {
		position: relative;
		display: inline-flex;
		width: 100%;
		min-width: 2.75rem;
		min-height: 2.75rem;
		align-items: center;
		justify-content: center;
		appearance: none;
		border: 0;
		border-radius: 0;
		background: transparent;
		padding: 0.375rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 0.9375rem;
		font-weight: 400;
		line-height: 1.25rem;
		cursor: pointer;
		transition:
			background-color 150ms ease,
			box-shadow 150ms ease,
			color 150ms ease;
	}

	.calendar-day:hover:not([data-disabled]):not([data-unavailable]):not([data-selected]) {
		background: var(--archive-layer);
	}

	.calendar-day[data-outside] {
		color: var(--marginal-note);
		font-style: italic;
	}

	.calendar-day[data-outside]::before {
		position: absolute;
		inset-block-start: 0.35rem;
		inset-inline-start: 0.35rem;
		width: 0.35rem;
		border-block-start: 1px solid currentColor;
		content: "";
		transform: rotate(-35deg);
	}

	.calendar-day[data-today] {
		box-shadow: inset 0 -3px 0 var(--register-mark);
		color: var(--register-mark);
		font-weight: 600;
	}

	.calendar-day[data-today]::after {
		position: absolute;
		inset-block-end: 0.3rem;
		width: 0.25rem;
		height: 0.25rem;
		border-radius: 999px;
		background: var(--register-mark);
		content: "";
	}

	.calendar-day[data-in-range] {
		background: var(--archive-layer);
		box-shadow:
			inset 0 2px 0 var(--time-marker),
			inset 0 -2px 0 var(--time-marker);
		font-weight: 600;
	}

	.calendar-day[data-range-start],
	.calendar-day[data-range-end],
	.calendar-day[data-selected]:not([data-in-range]) {
		border-radius: var(--radius-mark);
		background: var(--time-marker);
		box-shadow: inset 0 0 0 2px var(--reading-room);
		color: var(--reading-room);
		font-weight: 600;
	}

	.calendar-day[data-selected][data-today]::after {
		background: var(--register-mark);
		box-shadow: 0 0 0 1px var(--reading-room);
	}

	.calendar-day[data-preview-range]:not([data-selected]) {
		outline: 1px dashed var(--time-marker);
		outline-offset: -4px;
		background: color-mix(in srgb, var(--archive-layer) 64%, transparent);
	}

	.calendar-day[data-unavailable],
	.calendar-day[data-disabled] {
		color: var(--marginal-note);
		text-decoration: line-through;
		text-decoration-thickness: 1px;
		cursor: not-allowed;
	}

	.calendar-day[data-unavailable] {
		box-shadow: inset 0 0 0 1px var(--border-trace);
	}

	.calendar-day[data-hidden] {
		visibility: hidden;
		pointer-events: none;
	}

	.calendar-day:focus-visible {
		z-index: 1;
		outline: 3px solid var(--register-mark);
		outline-offset: 2px;
	}

	@media (prefers-reduced-motion: reduce) {
		.calendar-day {
			transition: none;
		}
	}
</style>
