import type { Snippet } from "svelte";
import type { HTMLAnchorAttributes, HTMLAttributes, HTMLButtonAttributes } from "svelte/elements";

export type MenuOpenReason =
	| "trigger"
	| "keyboard"
	| "pointer"
	| "outside"
	| "escape"
	| "selection"
	| "tab"
	| "native"
	| "programmatic";

export type DropdownMenuSide = "top" | "right" | "bottom" | "left";
export type DropdownMenuAlign = "start" | "center" | "end";
export type DropdownMenuFocusIntent = "first" | "last";

export interface DropdownMenuSelectEvent {
	readonly originalEvent: Event;
	readonly defaultPrevented: boolean;
	preventDefault(): void;
}

export interface DropdownMenuRootProps {
	open?: boolean;
	onOpenChange?: (open: boolean, reason: MenuOpenReason) => void;
	loop?: boolean;
	typeahead?: boolean;
	disabled?: boolean;
	children: Snippet;
}

export interface DropdownMenuTriggerProps extends Omit<
	HTMLButtonAttributes,
	"children" | "type" | "aria-haspopup" | "aria-expanded" | "aria-controls"
> {
	disabled?: boolean;
	children: Snippet;
	ref?: HTMLButtonElement | null;
}

interface DropdownMenuPositionProps {
	side?: DropdownMenuSide;
	align?: DropdownMenuAlign;
	sideOffset?: number;
	alignOffset?: number;
	collisionPadding?: number;
}

type NativeContentProps = Omit<HTMLAttributes<HTMLDivElement>, "children" | "role" | "popover">;

export interface DropdownMenuContentProps extends NativeContentProps, DropdownMenuPositionProps {
	children: Snippet;
	ref?: HTMLDivElement | null;
}

interface SharedDropdownMenuItemProps {
	disabled?: boolean;
	textValue?: string;
	closeOnSelect?: boolean;
	onSelect?: (event: DropdownMenuSelectEvent) => void;
	children: Snippet;
	ref?: HTMLButtonElement | HTMLAnchorElement | null;
	class?: HTMLButtonAttributes["class"];
}

type NativeMenuButtonProps = Omit<
	HTMLButtonAttributes,
	"children" | "class" | "disabled" | "href" | "ref" | "role" | "type" | "tabindex" | "aria-disabled"
>;
type NativeMenuAnchorProps = Omit<
	HTMLAnchorAttributes,
	"children" | "class" | "disabled" | "href" | "ref" | "role" | "type" | "tabindex" | "aria-disabled"
>;

export type DropdownMenuItemProps =
	| (SharedDropdownMenuItemProps & NativeMenuButtonProps & { href?: undefined })
	| (SharedDropdownMenuItemProps & NativeMenuAnchorProps & { href: string; type?: never });

export interface DropdownMenuGroupProps extends Omit<HTMLAttributes<HTMLDivElement>, "children" | "role"> {
	children: Snippet;
	ref?: HTMLDivElement | null;
}

export interface DropdownMenuLabelProps extends Omit<HTMLAttributes<HTMLDivElement>, "children"> {
	children: Snippet;
	ref?: HTMLDivElement | null;
}

export interface DropdownMenuSeparatorProps extends Omit<HTMLAttributes<HTMLDivElement>, "role"> {
	ref?: HTMLDivElement | null;
}

export interface DropdownMenuCheckboxGroupProps {
	value?: string[];
	onValueChange?: (value: string[]) => void;
	children: Snippet;
}

export interface DropdownMenuCheckboxItemProps extends Omit<
	HTMLButtonAttributes,
	"children" | "disabled" | "role" | "type" | "tabindex" | "aria-checked" | "aria-disabled"
> {
	value: string;
	disabled?: boolean;
	textValue?: string;
	closeOnSelect?: boolean;
	onSelect?: (event: DropdownMenuSelectEvent) => void;
	children: Snippet<[{ checked: boolean }]>;
	ref?: HTMLButtonElement | null;
}

export interface DropdownMenuRadioGroupProps {
	value?: string;
	onValueChange?: (value: string) => void;
	children: Snippet;
}

export interface DropdownMenuRadioItemProps extends Omit<
	HTMLButtonAttributes,
	"children" | "disabled" | "role" | "type" | "tabindex" | "aria-checked" | "aria-disabled"
> {
	value: string;
	disabled?: boolean;
	textValue?: string;
	closeOnSelect?: boolean;
	onSelect?: (event: DropdownMenuSelectEvent) => void;
	children: Snippet<[{ checked: boolean }]>;
	ref?: HTMLButtonElement | null;
}

export interface DropdownMenuSubProps {
	open?: boolean;
	onOpenChange?: (open: boolean, reason: MenuOpenReason) => void;
	children: Snippet;
}

export interface DropdownMenuSubTriggerProps extends Omit<
	HTMLButtonAttributes,
	| "children"
	| "type"
	| "role"
	| "tabindex"
	| "aria-disabled"
	| "aria-haspopup"
	| "aria-expanded"
	| "aria-controls"
> {
	disabled?: boolean;
	textValue?: string;
	openDelay?: number;
	children: Snippet;
	ref?: HTMLButtonElement | null;
}

export interface DropdownMenuSubContentProps extends NativeContentProps, DropdownMenuPositionProps {
	children: Snippet;
	ref?: HTMLDivElement | null;
}
