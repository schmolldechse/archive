import type { Snippet } from "svelte";
import type { HTMLAttributes, HTMLButtonAttributes, HTMLDialogAttributes } from "svelte/elements";

export type DialogOpenReason = "trigger" | "close" | "escape" | "outside" | "native" | "programmatic";
export type DialogSize = "small" | "medium" | "large" | "viewport";
export type DialogHeadingLevel = 2 | 3 | 4 | 5 | 6;

export interface DialogRootProps {
	open?: boolean;
	modal?: boolean;
	onOpenChange?: (open: boolean, reason: DialogOpenReason) => void;
	children: Snippet;
}

export interface DialogTriggerProps extends Omit<
	HTMLButtonAttributes,
	"children" | "type" | "aria-haspopup" | "aria-expanded" | "aria-controls"
> {
	disabled?: boolean;
	children: Snippet;
	ref?: HTMLButtonElement | null;
}

export interface DialogContentProps extends Omit<HTMLDialogAttributes, "children" | "open" | "closedby" | "aria-modal"> {
	size?: DialogSize;
	closeOnEscape?: boolean;
	closeOnOutsidePointer?: boolean;
	preventScroll?: boolean;
	restoreFocus?: boolean;
	initialFocus?: () => HTMLElement | null;
	returnFocus?: () => HTMLElement | null;
	children: Snippet;
	ref?: HTMLDialogElement | null;
}

export interface DialogTitleProps extends Omit<HTMLAttributes<HTMLHeadingElement>, "children"> {
	level?: DialogHeadingLevel;
	children: Snippet;
	ref?: HTMLHeadingElement | null;
}

export interface DialogDescriptionProps extends Omit<HTMLAttributes<HTMLParagraphElement>, "children"> {
	children: Snippet;
	ref?: HTMLParagraphElement | null;
}

export interface DialogActionsProps extends Omit<HTMLAttributes<HTMLDivElement>, "children"> {
	children: Snippet;
	ref?: HTMLDivElement | null;
}

export interface DialogCloseProps extends Omit<HTMLButtonAttributes, "children" | "type"> {
	disabled?: boolean;
	children: Snippet;
	ref?: HTMLButtonElement | null;
}
