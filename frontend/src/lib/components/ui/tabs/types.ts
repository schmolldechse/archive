import type { Snippet } from "svelte";
import type { HTMLAttributes, HTMLButtonAttributes } from "svelte/elements";

export type TabsOrientation = "horizontal" | "vertical";
export type TabsActivationMode = "automatic" | "manual";
export type TabsState = "active" | "inactive";

type NativeDivProps = Omit<HTMLAttributes<HTMLDivElement>, "children">;

export interface TabsRootProps extends NativeDivProps {
	value?: string;
	onValueChange?: (value: string) => void;
	orientation?: TabsOrientation;
	activationMode?: TabsActivationMode;
	loop?: boolean;
	disabled?: boolean;
	children: Snippet;
	ref?: HTMLDivElement | null;
}

interface TabsListSharedProps extends Omit<NativeDivProps, "aria-label" | "aria-labelledby" | "aria-orientation" | "role"> {
	children: Snippet;
	ref?: HTMLDivElement | null;
}

type TabsListAccessibleName =
	| {
			"aria-label": string;
			"aria-labelledby"?: string;
	  }
	| {
			"aria-label"?: string;
			"aria-labelledby": string;
	  };

export type TabsListProps = TabsListSharedProps & TabsListAccessibleName;

export interface TabsTriggerRenderProps {
	selected: boolean;
}

export interface TabsTriggerProps extends Omit<
	HTMLButtonAttributes,
	"aria-controls" | "aria-selected" | "children" | "disabled" | "id" | "role" | "tabindex" | "type"
> {
	value: string;
	disabled?: boolean;
	children: Snippet<[TabsTriggerRenderProps]>;
	ref?: HTMLButtonElement | null;
}

export interface TabsPanelProps extends Omit<
	HTMLAttributes<HTMLDivElement>,
	"aria-labelledby" | "children" | "hidden" | "id" | "role" | "tabindex"
> {
	value: string;
	forceMount?: boolean;
	children: Snippet;
	ref?: HTMLDivElement | null;
}
