import { DateTime, type Zone } from "luxon";

import type { CalendarRange, CalendarType, CalendarWeekdayFormat, CalendarWeekdayLabel } from "./types";

export function toCalendarDateKey(value: DateTime): string {
	return value.toFormat("yyyy-MM-dd");
}

export function toCalendarMonthKey(value: DateTime): string {
	return value.toFormat("yyyy-MM");
}

function assertSupportedYear(value: DateTime, name: string): void {
	if (value.year < 1 || value.year > 9999) {
		throw new Error(`Calendar ${name} must use a year from 0001 through 9999.`);
	}
}

export function assertValidCalendarDateTime(value: unknown, name: string): asserts value is DateTime {
	if (!DateTime.isDateTime(value) || !value.isValid) {
		throw new Error(`Calendar ${name} must be a valid Luxon DateTime.`);
	}

	assertSupportedYear(value, name);
}

export function compareCalendarDates(left: DateTime, right: DateTime): number {
	const leftKey = toCalendarDateKey(left);
	const rightKey = toCalendarDateKey(right);
	return leftKey < rightKey ? -1 : leftKey > rightKey ? 1 : 0;
}

export function isSameCalendarDate(left: DateTime | null | undefined, right: DateTime | null | undefined): boolean {
	return left != null && right != null && toCalendarDateKey(left) === toCalendarDateKey(right);
}

export function isSameCalendarMonth(left: DateTime, right: DateTime): boolean {
	return toCalendarMonthKey(left) === toCalendarMonthKey(right);
}

export function normalizeCalendarDate(value: DateTime, zone: string | Zone = value.zone): DateTime {
	return DateTime.fromObject({ year: value.year, month: value.month, day: value.day }, { zone });
}

export function normalizeCalendarMonth(value: DateTime, zone: string | Zone = value.zone): DateTime {
	return DateTime.fromObject({ year: value.year, month: value.month, day: 1 }, { zone });
}

export function validateCalendarInputs(
	visibleMonth: DateTime,
	today: DateTime | null,
	minValue: DateTime | undefined,
	maxValue: DateTime | undefined,
	value: unknown,
	type: CalendarType
): void {
	assertValidCalendarDateTime(visibleMonth, "visibleMonth");
	if (today !== null) assertValidCalendarDateTime(today, "today");
	if (minValue !== undefined) assertValidCalendarDateTime(minValue, "minValue");
	if (maxValue !== undefined) assertValidCalendarDateTime(maxValue, "maxValue");

	if (minValue !== undefined && maxValue !== undefined && compareCalendarDates(minValue, maxValue) > 0) {
		throw new Error("Calendar minValue must not be later than maxValue.");
	}

	if (type === "single") {
		if (value !== null) assertValidCalendarDateTime(value, "value");
		return;
	}

	if (value === null) return;
	if (DateTime.isDateTime(value) || typeof value !== "object" || !("start" in value) || !("end" in value)) {
		throw new Error("Calendar value must be a DateTime range or null when type is range.");
	}

	const range = value as { start: unknown; end: unknown };
	assertValidCalendarDateTime(range.start, "value.start");
	if (range.end !== null) {
		assertValidCalendarDateTime(range.end, "value.end");
		if (compareCalendarDates(range.end, range.start) < 0) {
			throw new Error("Calendar range end must not be earlier than its start.");
		}
	}
}

export function isSupportedCalendarDate(value: DateTime): boolean {
	return DateTime.isDateTime(value) && value.isValid && value.year >= 1 && value.year <= 9999;
}

export function addCalendarDays(date: DateTime, amount: number): DateTime {
	return normalizeCalendarDate(date).plus({ days: amount });
}

export function addCalendarMonthsToDate(date: DateTime, amount: number): DateTime {
	const current = normalizeCalendarDate(date);
	const targetStart = current.startOf("month").plus({ months: amount });
	const targetDay = Math.min(current.day, targetStart.daysInMonth ?? current.day);
	return targetStart.set({ day: targetDay });
}

export function addCalendarYearsToDate(date: DateTime, amount: number): DateTime {
	const current = normalizeCalendarDate(date);
	const targetStart = current.startOf("month").plus({ years: amount });
	const targetDay = Math.min(current.day, targetStart.daysInMonth ?? current.day);
	return targetStart.set({ day: targetDay });
}

export function addCalendarMonths(month: DateTime, amount: number): DateTime {
	return normalizeCalendarMonth(month).plus({ months: amount });
}

export function startOfCalendarWeek(date: DateTime, weekStartsOn: number): DateTime {
	const normalized = normalizeCalendarDate(date);
	const sundayBasedWeekday = normalized.weekday % 7;
	const offset = (sundayBasedWeekday - weekStartsOn + 7) % 7;
	return normalized.minus({ days: offset });
}

export function endOfCalendarWeek(date: DateTime, weekStartsOn: number): DateTime {
	return addCalendarDays(startOfCalendarWeek(date, weekStartsOn), 6);
}

export function createCalendarWeeks(month: DateTime, weekStartsOn: number, fixedWeeks: boolean): DateTime[][] {
	const monthStart = normalizeCalendarMonth(month);
	const firstDate = startOfCalendarWeek(monthStart, weekStartsOn);
	const leadingDays = ((monthStart.weekday % 7) - weekStartsOn + 7) % 7;
	const dayCount = fixedWeeks ? 42 : Math.ceil((leadingDays + (monthStart.daysInMonth ?? 31)) / 7) * 7;

	return Array.from({ length: dayCount / 7 }, (_, weekIndex) =>
		Array.from({ length: 7 }, (_, dayIndex) => addCalendarDays(firstDate, weekIndex * 7 + dayIndex))
	);
}

export function getCalendarWeekdayLabels(
	weekStartsOn: number,
	locale: string,
	format: CalendarWeekdayFormat
): CalendarWeekdayLabel[] {
	const sunday = DateTime.utc(2024, 1, 7);

	return Array.from({ length: 7 }, (_, index) => {
		const value = sunday.plus({ days: (weekStartsOn + index) % 7 }).setLocale(locale);

		return {
			key: toCalendarDateKey(value),
			short: value.toLocaleString({ weekday: format }),
			long: value.toLocaleString({ weekday: "long" })
		};
	});
}

export function formatCalendarMonth(month: DateTime, locale: string): string {
	return normalizeCalendarMonth(month).setLocale(locale).toLocaleString({ month: "long", year: "numeric" });
}

export function formatCalendarDate(date: DateTime, locale: string): string {
	return normalizeCalendarDate(date)
		.setLocale(locale)
		.toLocaleString({ weekday: "long", month: "long", day: "numeric", year: "numeric" });
}

export function formatCalendarNavigationLabel(direction: -1 | 1, targetMonth: DateTime, locale: string): string {
	const relative = new Intl.RelativeTimeFormat(locale, { numeric: "auto" }).format(direction, "month");
	const normalizedRelative = relative.charAt(0).toLocaleUpperCase(locale) + relative.slice(1);
	return `${normalizedRelative}: ${formatCalendarMonth(targetMonth, locale)}`;
}

export function isCalendarDateWithinBounds(
	date: DateTime,
	minValue: DateTime | undefined,
	maxValue: DateTime | undefined
): boolean {
	return (
		(minValue === undefined || compareCalendarDates(date, minValue) >= 0) &&
		(maxValue === undefined || compareCalendarDates(date, maxValue) <= 0)
	);
}

export function calendarMonthHasDateWithinBounds(
	month: DateTime,
	minValue: DateTime | undefined,
	maxValue: DateTime | undefined
): boolean {
	const first = normalizeCalendarMonth(month);
	const last = first.endOf("month");
	return (
		(minValue === undefined || compareCalendarDates(last, minValue) >= 0) &&
		(maxValue === undefined || compareCalendarDates(first, maxValue) <= 0)
	);
}

export function isCalendarDateInRange(date: DateTime, start: DateTime, end: DateTime, inclusive = true): boolean {
	const startComparison = compareCalendarDates(date, start);
	const endComparison = compareCalendarDates(date, end);
	return inclusive ? startComparison >= 0 && endComparison <= 0 : startComparison > 0 && endComparison < 0;
}

export function normalizeCalendarRange(start: DateTime, end: DateTime, zone: string | Zone = start.zone): CalendarRange {
	const normalizedStart = normalizeCalendarDate(start, zone);
	const normalizedEnd = normalizeCalendarDate(end, zone);
	return compareCalendarDates(normalizedStart, normalizedEnd) <= 0
		? { start: normalizedStart, end: normalizedEnd }
		: { start: normalizedEnd, end: normalizedStart };
}
