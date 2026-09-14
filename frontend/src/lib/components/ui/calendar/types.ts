import type { DateTime } from "luxon";
import type { Snippet } from "svelte";
import type { ClassValue, HTMLAttributes, HTMLButtonAttributes, HTMLTableAttributes } from "svelte/elements";

export type CalendarType = "single" | "range";
export type CalendarChangeSource = "keyboard" | "pointer" | "programmatic";
export type CalendarWeekdayFormat = "narrow" | "short" | "long";

export interface CalendarRange {
	start: DateTime;
	end: DateTime | null;
}

export interface CalendarHeadingState {
	visibleMonth: DateTime;
	label: string;
	previousAvailable: boolean;
	nextAvailable: boolean;
}

export interface CalendarDayState {
	date: DateTime;
	dayNumber: number;
	formattedLabel: string;
	outside: boolean;
	today: boolean;
	selected: boolean;
	unavailable: boolean;
	disabled: boolean;
	focused: boolean;
	rangeStart: boolean;
	rangeEnd: boolean;
	inRange: boolean;
	inPreviewRange: boolean;
	hidden: boolean;
}

export interface CalendarWeekdayLabel {
	key: string;
	short: string;
	long: string;
}

type NativeRootProps = Omit<HTMLAttributes<HTMLDivElement>, "children">;

export interface CalendarSharedProps extends NativeRootProps {
	visibleMonth: DateTime;
	onVisibleMonthChange?: (month: DateTime) => void;
	locale?: string;
	weekStartsOn?: 0 | 1 | 2 | 3 | 4 | 5 | 6;
	fixedWeeks?: boolean;
	showOutsideDays?: boolean;
	today?: DateTime | null;
	minValue?: DateTime;
	maxValue?: DateTime;
	isDateUnavailable?: (date: DateTime) => boolean;
	disabled?: boolean;
	readonly?: boolean;
	children: Snippet;
	ref?: HTMLDivElement | null;
}

export type CalendarSingleProps = CalendarSharedProps & {
	type: "single";
	value?: DateTime | null;
	onValueChange?: (value: DateTime | null, source: CalendarChangeSource) => void;
};

export type CalendarRangeProps = CalendarSharedProps & {
	type: "range";
	value?: CalendarRange | null;
	onValueChange?: (value: CalendarRange | null, source: CalendarChangeSource) => void;
};

export type CalendarRootProps = CalendarSingleProps | CalendarRangeProps;

export interface CalendarHeaderProps extends Omit<HTMLAttributes<HTMLDivElement>, "children"> {
	children: Snippet;
	ref?: HTMLDivElement | null;
}

type CalendarNavigationButtonProps = Omit<HTMLButtonAttributes, "aria-label" | "children" | "disabled" | "type"> & {
	children?: Snippet;
	ref?: HTMLButtonElement | null;
};

export type CalendarPreviousButtonProps = CalendarNavigationButtonProps;
export type CalendarNextButtonProps = CalendarNavigationButtonProps;

export interface CalendarHeadingProps extends Omit<HTMLAttributes<HTMLHeadingElement>, "children"> {
	children?: Snippet<[CalendarHeadingState]>;
	ref?: HTMLHeadingElement | null;
}

export interface CalendarGridProps extends Omit<HTMLTableAttributes, "children" | "role"> {
	weekdayFormat?: CalendarWeekdayFormat;
	day?: Snippet<[CalendarDayState]>;
	ref?: HTMLTableElement | null;
}

export interface CalendarDayProps extends Omit<
	HTMLButtonAttributes,
	"aria-current" | "aria-disabled" | "aria-label" | "aria-selected" | "children" | "disabled" | "tabindex" | "type"
> {
	date: DateTime;
	children?: Snippet<[CalendarDayState]>;
	class?: ClassValue;
	ref?: HTMLButtonElement | null;
}
