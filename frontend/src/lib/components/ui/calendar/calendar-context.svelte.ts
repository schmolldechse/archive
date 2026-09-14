import type { DateTime } from "luxon";
import { getContext, setContext } from "svelte";

import type {
	CalendarChangeSource,
	CalendarDayState,
	CalendarHeadingState,
	CalendarWeekdayFormat,
	CalendarWeekdayLabel
} from "./types";

export interface CalendarDayRegistration {
	readonly date: DateTime;
	readonly element: HTMLButtonElement | null;
}

export interface CalendarContext {
	readonly headingId: string;
	readonly visibleMonth: DateTime;
	readonly fixedWeeks: boolean;
	readonly disabled: boolean;
	readonly readonly: boolean;
	readonly weeks: DateTime[][];
	getDayState(date: DateTime): CalendarDayState;
	getHeadingState(): CalendarHeadingState;
	getNavigationLabel(direction: -1 | 1): string;
	getTabIndex(date: DateTime): number;
	getWeekdayLabels(format: CalendarWeekdayFormat): CalendarWeekdayLabel[];
	navigateDay(event: KeyboardEvent, date: DateTime): void;
	navigateMonth(direction: -1 | 1): void;
	registerDay(registration: CalendarDayRegistration): () => void;
	selectDate(date: DateTime, source: CalendarChangeSource): void;
	setFocusedDate(date: DateTime): void;
	setPreviewDate(date: DateTime | null): void;
	isNavigationAvailable(direction: -1 | 1): boolean;
}

const CALENDAR_CONTEXT_KEY = Symbol("ReusableUiCalendarContext");

export function setCalendarContext(value: CalendarContext): void {
	setContext(CALENDAR_CONTEXT_KEY, value);
}

export function getCalendarContext(componentName: string): CalendarContext {
	const value = getContext<CalendarContext>(CALENDAR_CONTEXT_KEY);

	if (!value) {
		throw new Error(`${componentName} must be rendered inside CalendarRoot.`);
	}

	return value;
}
