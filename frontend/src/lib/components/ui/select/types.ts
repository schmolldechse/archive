import type { Snippet } from "svelte";
import type {
	HTMLAttributes,
	HTMLButtonAttributes,
	HTMLLabelAttributes
} from "svelte/elements";

export type SelectItemValue = string;

export type SelectChangeSource = "keyboard" | "pointer" | "programmatic";
export type SelectSide = "top" | "bottom";
export type SelectAlign = "start" | "center" | "end";

export interface SelectValueState {
	value: SelectItemValue | null | SelectItemValue[];
	labels: string[];
	placeholder: boolean;
}

export interface SelectItemState {
	selected: boolean;
	highlighted: boolean;
	disabled: boolean;
}

type NativeRootProps = Omit<HTMLAttributes<HTMLDivElement>, "children">;

export interface SelectSharedProps extends NativeRootProps {
	open?: boolean;
	onOpenChange?: (open: boolean) => void;
	disabled?: boolean;
	required?: boolean;
	invalid?: boolean;
	name?: string;
	loop?: boolean;
	typeahead?: boolean;
	closeOnSelect?: boolean;
	children: Snippet;
	ref?: HTMLDivElement | null;
}

export type SelectSingleProps = SelectSharedProps & {
	type: "single";
	value?: SelectItemValue | null;
	allowDeselect?: boolean;
	onValueChange?: (value: SelectItemValue | null, source: SelectChangeSource) => void;
};

export type SelectMultipleProps = SelectSharedProps & {
	type: "multiple";
	value?: SelectItemValue[];
	allowDeselect?: never;
	onValueChange?: (value: SelectItemValue[], source: SelectChangeSource) => void;
};

export type SelectRootProps = SelectSingleProps | SelectMultipleProps;

export interface SelectLabelProps extends Omit<HTMLLabelAttributes, "children" | "for"> {
	children: Snippet;
	ref?: HTMLLabelElement | null;
}

export interface SelectTriggerProps extends Omit<
	HTMLButtonAttributes,
	| "children"
	| "type"
	| "disabled"
	| "aria-haspopup"
	| "aria-expanded"
	| "aria-controls"
	| "aria-required"
	| "aria-invalid"
> {
	children: Snippet;
	ref?: HTMLButtonElement | null;
}

export interface SelectValueProps extends Omit<HTMLAttributes<HTMLSpanElement>, "children"> {
	placeholder?: string;
	children?: Snippet<[SelectValueState]>;
	ref?: HTMLSpanElement | null;
}

export interface SelectContentProps extends Omit<
	HTMLAttributes<HTMLDivElement>,
	"children" | "popover" | "role" | "hidden" | "inert"
> {
	side?: SelectSide;
	align?: SelectAlign;
	sideOffset?: number;
	matchTriggerWidth?: boolean;
	collisionPadding?: number;
	children: Snippet;
	ref?: HTMLDivElement | null;
}

export interface SelectViewportProps extends Omit<
	HTMLAttributes<HTMLDivElement>,
	"children" | "role" | "tabindex" | "aria-multiselectable" | "aria-activedescendant"
> {
	children: Snippet;
	ref?: HTMLDivElement | null;
}

export interface SelectGroupProps extends Omit<HTMLAttributes<HTMLDivElement>, "children" | "role"> {
	children: Snippet;
	ref?: HTMLDivElement | null;
}

export interface SelectGroupLabelProps extends Omit<HTMLAttributes<HTMLDivElement>, "children"> {
	children: Snippet;
	ref?: HTMLDivElement | null;
}

export interface SelectItemProps extends Omit<
	HTMLAttributes<HTMLDivElement>,
	"children" | "role" | "tabindex" | "aria-selected" | "aria-disabled"
> {
	value: SelectItemValue;
	textValue?: string;
	disabled?: boolean;
	children: Snippet<[SelectItemState]>;
	ref?: HTMLDivElement | null;
}

export interface SelectSeparatorProps extends Omit<
	HTMLAttributes<HTMLDivElement>,
	"role" | "aria-orientation"
> {
	ref?: HTMLDivElement | null;
}

export interface SelectDescriptionProps extends Omit<HTMLAttributes<HTMLParagraphElement>, "children"> {
	children: Snippet;
	ref?: HTMLParagraphElement | null;
}

export interface SelectErrorProps extends Omit<HTMLAttributes<HTMLParagraphElement>, "children"> {
	children: Snippet;
	ref?: HTMLParagraphElement | null;
}
