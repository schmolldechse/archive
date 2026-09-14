<script lang="ts">
	import CalendarDay from "./CalendarDay.svelte";
	import { getCalendarContext } from "./calendar-context.svelte";
	import { toCalendarDateKey, toCalendarMonthKey } from "./helpers";
	import type { CalendarGridProps } from "./types";

	let {
		weekdayFormat = "short",
		day,
		ref = $bindable(null),
		class: className,
		"aria-labelledby": ariaLabelledby,
		...restProps
	}: CalendarGridProps = $props();

	const calendar = getCalendarContext("CalendarGrid");
	const weeks = $derived(calendar.weeks);
	const weekdays = $derived(calendar.getWeekdayLabels(weekdayFormat));
</script>

<table
	{...restProps}
	bind:this={ref}
	class={["calendar-grid", className]}
	role="grid"
	aria-labelledby={ariaLabelledby ?? calendar.headingId}
	aria-disabled={calendar.disabled ? "true" : undefined}
	aria-readonly={calendar.readonly ? "true" : undefined}
	data-calendar-grid
	data-month={toCalendarMonthKey(calendar.visibleMonth)}
	data-fixed-weeks={calendar.fixedWeeks ? "" : undefined}
>
	<thead>
		<tr>
			{#each weekdays as weekday (weekday.key)}
				<th scope="col" aria-label={weekday.long}>
					{#if weekdayFormat === "long"}
						{weekday.short}
					{:else}
						<abbr title={weekday.long}>{weekday.short}</abbr>
					{/if}
				</th>
			{/each}
		</tr>
	</thead>
	<tbody>
		{#each weeks as week (toCalendarDateKey(week[0]))}
			<tr>
				{#each week as date (toCalendarDateKey(date))}
					<CalendarDay {date} children={day} />
				{/each}
			</tr>
		{/each}
	</tbody>
</table>

<style>
	.calendar-grid {
		width: 100%;
		min-width: 19.25rem;
		border-collapse: collapse;
		border-spacing: 0;
		table-layout: fixed;
		font-family: var(--font-interface);
		font-variant-numeric: tabular-nums;
	}

	.calendar-grid th {
		height: 2.25rem;
		padding: 0.25rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.04em;
		line-height: 1.33;
		text-align: center;
		text-transform: uppercase;
	}

	.calendar-grid abbr {
		text-decoration: none;
		cursor: help;
	}
</style>
