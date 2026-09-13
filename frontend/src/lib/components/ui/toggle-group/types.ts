import type { Snippet } from "svelte";
import type { HTMLAttributes, HTMLButtonAttributes } from "svelte/elements";

export type ToggleGroupOrientation = "horizontal" | "vertical";
export type ToggleGroupSemanticRole = "group" | "toolbar";
export type ToggleGroupState = "on" | "off";

type NativeDivProps = Omit<
	HTMLAttributes<HTMLDivElement>,
	"aria-label" | "aria-labelledby" | "aria-orientation" | "children" | "role"
>;

interface ToggleGroupRootSharedProps extends NativeDivProps {
	selectionRequired?: boolean;
	orientation?: ToggleGroupOrientation;
	loop?: boolean;
	rovingFocus?: boolean;
	semanticRole?: ToggleGroupSemanticRole;
	disabled?: boolean;
	children: Snippet;
	ref?: HTMLDivElement | null;
}

type ToggleGroupAccessibleName =
	| {
			"aria-label": string;
			"aria-labelledby"?: string;
	  }
	| {
			"aria-label"?: string;
			"aria-labelledby": string;
	  };

export type SingleToggleGroupProps = ToggleGroupRootSharedProps &
	ToggleGroupAccessibleName & {
		type: "single";
		value?: string | null;
		onValueChange?: (value: string | null) => void;
	};

export type MultipleToggleGroupProps = ToggleGroupRootSharedProps &
	ToggleGroupAccessibleName & {
		type: "multiple";
		value?: string[];
		onValueChange?: (value: string[]) => void;
	};

export type ToggleGroupRootProps = SingleToggleGroupProps | MultipleToggleGroupProps;

export interface ToggleGroupItemRenderProps {
	pressed: boolean;
}

export interface ToggleGroupItemProps extends Omit<
	HTMLButtonAttributes,
	"aria-pressed" | "children" | "disabled" | "role" | "tabindex" | "type" | "value"
> {
	value: string;
	disabled?: boolean;
	children: Snippet<[ToggleGroupItemRenderProps]>;
	ref?: HTMLButtonElement | null;
}
