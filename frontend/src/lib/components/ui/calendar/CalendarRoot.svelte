<script lang="ts">
	import { DateTime } from "luxon";
	import { tick } from "svelte";

	import {
		addCalendarDays,
		addCalendarMonths,
		addCalendarMonthsToDate,
		addCalendarYearsToDate,
		calendarMonthHasDateWithinBounds,
		createCalendarWeeks,
		endOfCalendarWeek,
		formatCalendarDate,
		formatCalendarMonth,
		formatCalendarNavigationLabel,
		getCalendarWeekdayLabels,
		isCalendarDateInRange,
		isCalendarDateWithinBounds,
		isSameCalendarDate,
		isSameCalendarMonth,
		isSupportedCalendarDate,
		normalizeCalendarDate,
		normalizeCalendarMonth,
		normalizeCalendarRange,
		startOfCalendarWeek,
		validateCalendarInputs
	} from "./helpers";
	import { setCalendarContext, type CalendarDayRegistration } from "./calendar-context.svelte";
	import type {
		CalendarChangeSource,
		CalendarDayState,
		CalendarRange,
		CalendarRangeProps,
		CalendarRootProps,
		CalendarSingleProps
	} from "./types";

	let {
		type,
		value = $bindable(null),
		onValueChange,
		visibleMonth = $bindable(),
		onVisibleMonthChange,
		locale = "en",
		weekStartsOn = 1,
		fixedWeeks = true,
		showOutsideDays = true,
		today = null,
		minValue,
		maxValue,
		isDateUnavailable = () => false,
		disabled = false,
		readonly = false,
		children,
		ref = $bindable(null),
		class: className,
		...restProps
	}: CalendarRootProps = $props();

	const componentId = $props.id();
	const headingId = `${componentId}-heading`;

	let focusedDate = $state<DateTime | null>(null);
	let previewDate = $state<DateTime | null>(null);
	let dayRegistrations: CalendarDayRegistration[] = [];

	const validated = $derived.by(() => {
		validateCalendarInputs(visibleMonth, today, minValue, maxValue, value, type);
		return true;
	});
	const currentMonth = $derived.by(() => {
		void validated;
		return normalizeCalendarMonth(visibleMonth);
	});

	const weeks = $derived.by(() => {
		return createCalendarWeeks(currentMonth, weekStartsOn, fixedWeeks);
	});

	const flatDates = $derived(weeks.flat());
	const range = $derived<CalendarRange | null>(
		type === "range" && value !== null && !DateTime.isDateTime(value) ? value : null
	);

	const isDateHidden = (date: DateTime): boolean =>
		!isSameCalendarMonth(date, currentMonth) && (!showOutsideDays || !isSupportedCalendarDate(date));

	const isDateDisabled = (date: DateTime): boolean =>
		disabled || !isSupportedCalendarDate(date) || !isCalendarDateWithinBounds(date, minValue, maxValue);

	const isDateAvailable = (date: DateTime): boolean => !isDateHidden(date) && !isDateDisabled(date) && !isDateUnavailable(date);

	const findDate = (date: DateTime | null | undefined, candidates: DateTime[]): DateTime | undefined =>
		date == null ? undefined : candidates.find((candidate) => isSameCalendarDate(candidate, date));

	const findAvailableDate = (date: DateTime | null | undefined, candidates: DateTime[]): DateTime | undefined => {
		const candidate = findDate(date, candidates);
		return candidate !== undefined && isDateAvailable(candidate) ? candidate : undefined;
	};

	const getSelectedDateInMonth = (): DateTime | undefined => {
		if (type === "single" && DateTime.isDateTime(value) && isSameCalendarMonth(value, currentMonth)) {
			return findAvailableDate(value, flatDates);
		}

		if (range?.start && isSameCalendarMonth(range.start, currentMonth)) {
			return findAvailableDate(range.start, flatDates);
		}

		return undefined;
	};

	const getTabStopDate = (): DateTime | undefined => {
		void validated;

		const focusedCandidate = findDate(focusedDate, flatDates);
		if (focusedCandidate !== undefined && !isDateHidden(focusedCandidate) && !disabled) return focusedCandidate;

		return (
			getSelectedDateInMonth() ??
			(today !== null && isSameCalendarMonth(today, currentMonth) ? findAvailableDate(today, flatDates) : undefined) ??
			flatDates.find(isDateAvailable)
		);
	};

	const tabStopDate = $derived.by(getTabStopDate);

	const getInitialDateForMonth = (month: DateTime): DateTime | null => {
		const monthDates = createCalendarWeeks(month, weekStartsOn, fixedWeeks).flat();
		const isAvailableInTarget = (date: DateTime): boolean =>
			(showOutsideDays || isSameCalendarMonth(date, month)) &&
			isCalendarDateWithinBounds(date, minValue, maxValue) &&
			!isDateUnavailable(date);
		const findTargetDate = (date: DateTime | null | undefined): DateTime | undefined =>
			date == null
				? undefined
				: monthDates.find((candidate) => isSameCalendarDate(candidate, date) && isAvailableInTarget(candidate));

		if (type === "single" && DateTime.isDateTime(value) && isSameCalendarMonth(value, month)) {
			const selectedDate = findTargetDate(value);
			if (selectedDate !== undefined) return selectedDate;
		}

		if (range?.start && isSameCalendarMonth(range.start, month)) {
			const rangeStart = findTargetDate(range.start);
			if (rangeStart !== undefined) return rangeStart;
		}

		if (today !== null && isSameCalendarMonth(today, month)) {
			const todayDate = findTargetDate(today);
			if (todayDate !== undefined) return todayDate;
		}

		return monthDates.find(isAvailableInTarget) ?? null;
	};

	const getDayState = (date: DateTime): CalendarDayState => {
		void validated;

		const outside = !isSameCalendarMonth(date, currentMonth);
		const unavailable = isSupportedCalendarDate(date) ? isDateUnavailable(date) : false;
		const dayDisabled = isDateDisabled(date);
		const rangeStart = isSameCalendarDate(range?.start, date);
		const rangeEnd = isSameCalendarDate(range?.end, date);
		const inRange =
			range?.end !== null && range?.end !== undefined ? isCalendarDateInRange(date, range.start, range.end, false) : false;
		const selected =
			type === "single" && DateTime.isDateTime(value)
				? isSameCalendarDate(value, date)
				: Boolean(rangeStart || rangeEnd || inRange);
		const previewTarget = range?.end === null ? (previewDate ?? focusedDate) : null;
		const previewRange =
			previewTarget === null || range === null ? null : normalizeCalendarRange(range.start, previewTarget, currentMonth.zone);
		const inPreviewRange =
			previewRange === null
				? false
				: !isSameCalendarDate(date, range?.start) &&
					isCalendarDateInRange(date, previewRange.start, previewRange.end ?? previewRange.start);

		const statusLabels = [
			rangeStart ? "Range start" : null,
			rangeEnd ? "Range end" : null,
			unavailable || dayDisabled ? "Unavailable" : null
		].filter(Boolean);
		const baseLabel = formatCalendarDate(date, locale);

		return {
			date,
			dayNumber: date.day,
			formattedLabel: statusLabels.length > 0 ? `${baseLabel}, ${statusLabels.join(", ")}` : baseLabel,
			outside,
			today: isSameCalendarDate(today, date),
			selected,
			unavailable,
			disabled: dayDisabled,
			focused: isSameCalendarDate(tabStopDate, date),
			rangeStart,
			rangeEnd,
			inRange,
			inPreviewRange,
			hidden: outside && !showOutsideDays
		};
	};

	const commitVisibleMonth = (nextMonth: DateTime): void => {
		if (isSameCalendarMonth(currentMonth, nextMonth)) return;
		const normalizedMonth = normalizeCalendarMonth(nextMonth, currentMonth.zone);
		visibleMonth = normalizedMonth;
		onVisibleMonthChange?.(normalizedMonth);
	};

	const isNavigationAvailable = (direction: -1 | 1): boolean => {
		void validated;
		if (disabled) return false;

		const targetMonth = addCalendarMonths(currentMonth, direction);
		return isSupportedCalendarDate(targetMonth) && calendarMonthHasDateWithinBounds(targetMonth, minValue, maxValue);
	};

	const navigateMonth = (direction: -1 | 1): void => {
		if (!isNavigationAvailable(direction)) return;

		const targetMonth = addCalendarMonths(currentMonth, direction);
		focusedDate = getInitialDateForMonth(targetMonth);
		previewDate = null;
		commitVisibleMonth(targetMonth);
	};

	const commitSingleValue = (date: DateTime, source: CalendarChangeSource): void => {
		if (DateTime.isDateTime(value) && isSameCalendarDate(value, date)) return;
		const normalizedDate = normalizeCalendarDate(date, currentMonth.zone);
		value = normalizedDate;
		(onValueChange as CalendarSingleProps["onValueChange"] | undefined)?.(normalizedDate, source);
	};

	const commitRangeValue = (nextValue: CalendarRange, source: CalendarChangeSource): void => {
		const normalizedValue = {
			start: normalizeCalendarDate(nextValue.start, currentMonth.zone),
			end: nextValue.end === null ? null : normalizeCalendarDate(nextValue.end, currentMonth.zone)
		};
		value = normalizedValue;
		(onValueChange as CalendarRangeProps["onValueChange"] | undefined)?.(normalizedValue, source);
	};

	const selectDate = (date: DateTime, source: CalendarChangeSource): void => {
		void validated;
		if (readonly || isDateDisabled(date) || isDateUnavailable(date) || isDateHidden(date)) return;

		if (type === "single") {
			commitSingleValue(date, source);
			return;
		}

		if (range === null || range.end !== null) {
			commitRangeValue({ start: date, end: null }, source);
		} else {
			commitRangeValue(normalizeCalendarRange(range.start, date, currentMonth.zone), source);
		}

		previewDate = null;
	};

	const focusRegisteredDay = async (date: DateTime): Promise<void> => {
		await tick();
		dayRegistrations.find((registration) => isSameCalendarDate(registration.date, date))?.element?.focus();
	};

	const moveFocus = (date: DateTime): void => {
		if (!isSupportedCalendarDate(date)) return;

		const targetMonth = normalizeCalendarMonth(date);
		if (!isSameCalendarMonth(targetMonth, currentMonth) && !calendarMonthHasDateWithinBounds(targetMonth, minValue, maxValue)) {
			return;
		}

		focusedDate = date;
		previewDate = null;
		if (!isSameCalendarMonth(targetMonth, currentMonth)) commitVisibleMonth(targetMonth);
		void focusRegisteredDay(date);
	};

	const navigateDay = (event: KeyboardEvent, date: DateTime): void => {
		if (disabled || event.altKey || event.ctrlKey || event.metaKey) return;

		let targetDate: DateTime | null = null;
		const direction = event.currentTarget instanceof Element ? getComputedStyle(event.currentTarget).direction : "ltr";

		switch (event.key) {
			case "ArrowLeft":
				targetDate = addCalendarDays(date, direction === "rtl" ? 1 : -1);
				break;
			case "ArrowRight":
				targetDate = addCalendarDays(date, direction === "rtl" ? -1 : 1);
				break;
			case "ArrowUp":
				targetDate = addCalendarDays(date, -7);
				break;
			case "ArrowDown":
				targetDate = addCalendarDays(date, 7);
				break;
			case "Home":
				targetDate = startOfCalendarWeek(date, weekStartsOn);
				break;
			case "End":
				targetDate = endOfCalendarWeek(date, weekStartsOn);
				break;
			case "PageUp":
				targetDate = event.shiftKey ? addCalendarYearsToDate(date, -1) : addCalendarMonthsToDate(date, -1);
				break;
			case "PageDown":
				targetDate = event.shiftKey ? addCalendarYearsToDate(date, 1) : addCalendarMonthsToDate(date, 1);
				break;
			case "Enter":
			case " ":
				event.preventDefault();
				selectDate(date, "keyboard");
				return;
			default:
				return;
		}

		event.preventDefault();
		if (targetDate !== null) moveFocus(targetDate);
	};

	const registerDay = (registration: CalendarDayRegistration): (() => void) => {
		dayRegistrations = [...dayRegistrations, registration];

		return () => {
			dayRegistrations = dayRegistrations.filter((candidate) => candidate !== registration);
		};
	};

	setCalendarContext({
		headingId,
		get disabled() {
			return disabled;
		},
		get readonly() {
			return readonly;
		},
		get visibleMonth() {
			return currentMonth;
		},
		get fixedWeeks() {
			return fixedWeeks;
		},
		get weeks() {
			return weeks;
		},
		getDayState,
		getHeadingState() {
			return {
				visibleMonth: currentMonth,
				label: formatCalendarMonth(currentMonth, locale),
				previousAvailable: isNavigationAvailable(-1),
				nextAvailable: isNavigationAvailable(1)
			};
		},
		getNavigationLabel(direction) {
			return formatCalendarNavigationLabel(direction, addCalendarMonths(currentMonth, direction), locale);
		},
		getTabIndex(date) {
			return isSameCalendarDate(tabStopDate, date) ? 0 : -1;
		},
		getWeekdayLabels(format) {
			return getCalendarWeekdayLabels(weekStartsOn, locale, format);
		},
		navigateDay,
		navigateMonth,
		registerDay,
		selectDate,
		setFocusedDate(date) {
			if (!disabled) focusedDate = date;
		},
		setPreviewDate(date) {
			if (!disabled) previewDate = date;
		},
		isNavigationAvailable
	});
</script>

<div
	{...restProps}
	bind:this={ref}
	class={["calendar-root", className]}
	data-component="calendar"
	data-calendar-root
	data-type={type}
	data-disabled={disabled ? "" : undefined}
	data-readonly={readonly ? "" : undefined}
	data-range-incomplete={range?.end === null ? "" : undefined}
>
	{#if validated}
		{@render children()}
	{/if}
</div>

<style>
	.calendar-root {
		display: inline-grid;
		width: 19.375rem;
		min-width: 19.375rem;
		max-width: 100%;
		justify-self: start;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--reading-room);
		color: var(--reading-ink);
		font-family: var(--font-interface);
		overflow-x: auto;
	}

	.calendar-root[data-disabled] {
		cursor: not-allowed;
	}

	.calendar-root[data-readonly] {
		box-shadow: inset 3px 0 0 var(--border-trace);
	}

	.calendar-root[data-readonly] :global([data-calendar-day]) {
		cursor: default;
	}
</style>
