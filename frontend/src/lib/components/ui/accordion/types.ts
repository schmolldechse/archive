import type { Snippet } from "svelte";
import type { HTMLAttributes, HTMLButtonAttributes } from "svelte/elements";

export type AccordionHeadingLevel = 2 | 3 | 4 | 5 | 6;
export type AccordionState = "open" | "closed";

type NativeDivProps = Omit<HTMLAttributes<HTMLDivElement>, "children">;

interface AccordionRootSharedProps extends NativeDivProps {
	disabled?: boolean;
	headingLevel?: AccordionHeadingLevel;
	keyboardNavigation?: boolean;
	loop?: boolean;
	children: Snippet;
	ref?: HTMLDivElement | null;
}

export type SingleAccordionProps = AccordionRootSharedProps & {
	type: "single";
	value?: string | null;
	onValueChange?: (value: string | null) => void;
	collapsible?: boolean;
};

export type MultipleAccordionProps = AccordionRootSharedProps & {
	type: "multiple";
	value?: string[];
	onValueChange?: (value: string[]) => void;
	collapsible?: never;
};

export type AccordionRootProps = SingleAccordionProps | MultipleAccordionProps;

export interface AccordionItemRenderProps {
	open: boolean;
}

export interface AccordionItemProps extends NativeDivProps {
	value: string;
	disabled?: boolean;
	children: Snippet<[AccordionItemRenderProps]>;
	ref?: HTMLDivElement | null;
}

export interface AccordionHeaderProps extends Omit<HTMLAttributes<HTMLHeadingElement>, "children"> {
	level?: AccordionHeadingLevel;
	children: Snippet;
	ref?: HTMLHeadingElement | null;
}

export interface AccordionTriggerProps extends Omit<HTMLButtonAttributes, "children" | "disabled"> {
	disabled?: boolean;
	children: Snippet<[AccordionItemRenderProps]>;
	ref?: HTMLButtonElement | null;
}

export interface AccordionContentProps extends Omit<HTMLAttributes<HTMLDivElement>, "children" | "hidden"> {
	forceMount?: boolean;
	hiddenUntilFound?: boolean;
	region?: boolean;
	children: Snippet;
	ref?: HTMLDivElement | null;
}
