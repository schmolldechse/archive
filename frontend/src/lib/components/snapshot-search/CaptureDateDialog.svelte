<script lang="ts">
	import {
		CalendarGrid,
		CalendarHeader,
		CalendarHeading,
		CalendarNextButton,
		CalendarPreviousButton,
		CalendarRoot
	} from "$lib/components/ui/calendar";
	import { DialogContent, type DialogPosition } from "$lib/components/ui/dialog";
	import { DateTime } from "luxon";
	import { parseDate } from "./search-state";

	let {
		id,
		title,
		selected,
		visibleMonth,
		today,
		minValue,
		maxValue,
		position = "viewport",
		onVisibleMonthChange,
		onSelect
	}: {
		id: string;
		title: string;
		selected: DateTime | null;
		visibleMonth: DateTime;
		today: DateTime;
		minValue?: DateTime;
		maxValue?: DateTime;
		position?: DialogPosition;
		onVisibleMonthChange: (value: DateTime) => void;
		onSelect: (value: DateTime) => void;
	} = $props();

	let calendar = $state<HTMLDivElement | null>(null);

	function confirmSelectedDay(event: MouseEvent | KeyboardEvent): void {
		if (event instanceof KeyboardEvent && event.key !== "Enter" && event.key !== " ") return;
		const day =
			event.target instanceof Element ? event.target.closest<HTMLButtonElement>("[data-calendar-day][data-selected]") : null;

		if (day && day.getAttribute("aria-disabled") !== "true") {
			const date = parseDate(day.dataset.date ?? "");
			if (date) onSelect(date);
		}
	}
</script>

<DialogContent
	{id}
	size="small"
	{position}
	sideOffset={4}
	collisionPadding={4}
	closeOnOutsidePointer
	class="capture-date-dialog"
	aria-label={title}
	initialFocus={() => calendar?.querySelector<HTMLButtonElement>('[data-calendar-day][tabindex="0"]') ?? null}
>
	<CalendarRoot
		type="single"
		fixedWeeks={false}
		value={selected}
		{visibleMonth}
		{today}
		{minValue}
		{maxValue}
		bind:ref={calendar}
		{onVisibleMonthChange}
		onValueChange={(value) => {
			if (value) onSelect(value);
		}}
		class="capture-date-calendar"
	>
		<CalendarHeader><CalendarPreviousButton /><CalendarHeading /><CalendarNextButton /></CalendarHeader>
		<CalendarGrid weekdayFormat="narrow" onclick={confirmSelectedDay} onkeydown={confirmSelectedDay} />
	</CalendarRoot>
</DialogContent>

<style>
	:global([data-dialog-content].capture-date-dialog) {
		width: min(calc(100% - 2rem), 19.375rem);
		border: 0;
		border-radius: var(--radius-control);
		background: transparent;
		padding: 0;
	}

	:global([data-dialog-content].capture-date-dialog[data-modal="true"]) {
		position: fixed;
		inset: 0;
		margin: auto;
	}

	:global(.capture-date-dialog [data-calendar-root].capture-date-calendar) {
		display: grid;
		width: 100%;
		min-width: 0;
		justify-self: stretch;
	}

	:global(.capture-date-dialog .capture-date-calendar [data-calendar-grid]) {
		min-width: 0;
	}

	@media (max-width: 23rem) {
		:global([data-dialog-content].capture-date-dialog) {
			width: calc(100% - 1rem);
		}
	}
</style>
