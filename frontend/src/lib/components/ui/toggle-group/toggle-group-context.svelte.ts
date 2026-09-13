import { getContext, setContext } from "svelte";

import type { ToggleGroupOrientation } from "./types";

export interface ToggleGroupItemRegistration {
	readonly value: string;
	readonly disabled: boolean;
	readonly element: HTMLButtonElement | null;
}

export interface ToggleGroupContext {
	readonly disabled: boolean;
	readonly loop: boolean;
	readonly orientation: ToggleGroupOrientation;
	readonly rovingFocus: boolean;
	focusItem(value: string, itemDisabled: boolean): void;
	getTabIndex(value: string, itemDisabled: boolean): number | undefined;
	isItemDisabled(value: string, itemDisabled: boolean): boolean;
	isPressed(value: string): boolean;
	navigate(event: KeyboardEvent, value: string): void;
	registerItem(item: ToggleGroupItemRegistration): () => void;
	toggle(value: string, itemDisabled: boolean): void;
}

const TOGGLE_GROUP_CONTEXT_KEY = Symbol("ReusableUiToggleGroupContext");

export function setToggleGroupContext(value: ToggleGroupContext): void {
	setContext(TOGGLE_GROUP_CONTEXT_KEY, value);
}

export function getToggleGroupContext(): ToggleGroupContext {
	const value = getContext<ToggleGroupContext>(TOGGLE_GROUP_CONTEXT_KEY);

	if (!value) {
		throw new Error("ToggleGroupItem must be rendered inside ToggleGroupRoot.");
	}

	return value;
}
