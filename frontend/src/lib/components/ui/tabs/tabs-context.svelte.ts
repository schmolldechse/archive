import { getContext, setContext } from "svelte";

import type { TabsActivationMode, TabsOrientation } from "./types";

export interface TabsTriggerRegistration {
	readonly value: string;
	readonly disabled: boolean;
	readonly element: HTMLButtonElement | null;
}

export interface TabsPanelRegistration {
	readonly value: string;
}

export interface TabsContext {
	readonly activationMode: TabsActivationMode;
	readonly activeValue: string | undefined;
	readonly disabled: boolean;
	readonly loop: boolean;
	readonly orientation: TabsOrientation;
	activate(value: string, triggerDisabled: boolean): void;
	blurTrigger(nextTarget: EventTarget | null): void;
	focusTrigger(value: string, triggerDisabled: boolean): void;
	getDefaultPanelId(value: string): string;
	getDefaultTriggerId(value: string): string;
	getPanelId(value: string): string;
	getTriggerId(value: string): string;
	isSelected(value: string): boolean;
	isTabStop(value: string, triggerDisabled: boolean): boolean;
	isTriggerDisabled(value: string, triggerDisabled: boolean): boolean;
	navigate(event: KeyboardEvent, value: string): void;
	registerPanel(panel: TabsPanelRegistration): () => void;
	registerTrigger(trigger: TabsTriggerRegistration): () => void;
}

interface TabsListContext {
	readonly list: true;
}

const TABS_CONTEXT_KEY = Symbol("ReusableUiTabsContext");
const TABS_LIST_CONTEXT_KEY = Symbol("ReusableUiTabsListContext");

export function setTabsContext(value: TabsContext): void {
	setContext(TABS_CONTEXT_KEY, value);
}

export function getTabsContext(componentName: string): TabsContext {
	const value = getContext<TabsContext>(TABS_CONTEXT_KEY);

	if (!value) {
		throw new Error(`${componentName} must be rendered inside TabsRoot.`);
	}

	return value;
}

export function setTabsListContext(): void {
	setContext<TabsListContext>(TABS_LIST_CONTEXT_KEY, { list: true });
}

export function getTabsListContext(componentName: string): TabsListContext {
	const value = getContext<TabsListContext>(TABS_LIST_CONTEXT_KEY);

	if (!value) {
		throw new Error(`${componentName} must be rendered inside TabsList.`);
	}

	return value;
}
