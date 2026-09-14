import { getContext, setContext } from "svelte";

import type {
	SelectChangeSource,
	SelectItemValue,
	SelectValueState
} from "./types";

export type SelectHighlightIntent = "selected-or-first" | "selected-or-last" | "first" | "last";

export interface RegisteredSelectItem {
	readonly id: string;
	readonly value: SelectItemValue;
	getDisabled(): boolean;
	getElement(): HTMLDivElement | null;
	getTextValue(): string;
}

type ElementGetter<T extends HTMLElement> = () => T | null;
type IdGetter = () => string;

export interface SelectContext {
	readonly closeOnSelect: boolean;
	readonly currentValue: SelectItemValue | null | SelectItemValue[];
	readonly defaultDescriptionId: string;
	readonly defaultErrorId: string;
	readonly defaultLabelId: string;
	readonly descriptionId: string | undefined;
	readonly disabled: boolean;
	readonly empty: boolean;
	readonly errorId: string | undefined;
	readonly highlightedId: string | null;
	readonly invalid: boolean;
	readonly labelId: string | undefined;
	readonly listboxId: string;
	readonly loop: boolean;
	readonly open: boolean;
	readonly required: boolean;
	readonly selectedLabels: string[];
	readonly triggerId: string;
	readonly type: "single" | "multiple";
	readonly typeahead: boolean;
	consumeRestoreFocus(): boolean;
	focusViewport(): void;
	getTrigger(): HTMLButtonElement | null;
	getViewport(): HTMLDivElement | null;
	handleTypeahead(key: string): void;
	isItemSelected(value: SelectItemValue): boolean;
	moveHighlight(offset: number): void;
	reconcileItems(): void;
	registerDescriptionId(getId: IdGetter): () => void;
	registerErrorId(getId: IdGetter): () => void;
	registerItem(item: RegisteredSelectItem): () => void;
	registerLabelId(getId: IdGetter): () => void;
	registerListboxId(getId: IdGetter): () => void;
	registerTrigger(getElement: ElementGetter<HTMLButtonElement>): () => void;
	registerTriggerId(getId: IdGetter): () => void;
	registerViewport(getElement: ElementGetter<HTMLDivElement>): () => void;
	requestOpenChange(open: boolean, intent?: SelectHighlightIntent, restoreFocus?: boolean): void;
	restoreTriggerFocus(): void;
	selectHighlighted(source: SelectChangeSource): void;
	selectItem(value: SelectItemValue, source: SelectChangeSource): void;
	setHighlighted(id: string): void;
	synchronizeProgrammaticOpen(): void;
	valueState(): SelectValueState;
}

export interface SelectGroupContext {
	readonly defaultLabelId: string;
	readonly labelId: string | undefined;
	registerLabelId(getId: IdGetter): () => void;
}

const SELECT_CONTEXT_KEY = Symbol("ReusableUiSelectContext");
const SELECT_GROUP_CONTEXT_KEY = Symbol("ReusableUiSelectGroupContext");

export function setSelectContext(value: SelectContext): void {
	setContext(SELECT_CONTEXT_KEY, value);
}

export function getSelectContext(componentName: string): SelectContext {
	const value = getContext<SelectContext>(SELECT_CONTEXT_KEY);

	if (!value) {
		throw new Error(`${componentName} must be rendered inside SelectRoot.`);
	}

	return value;
}

export function setSelectGroupContext(value: SelectGroupContext): void {
	setContext(SELECT_GROUP_CONTEXT_KEY, value);
}

export function getSelectGroupContext(componentName: string): SelectGroupContext {
	const value = getContext<SelectGroupContext>(SELECT_GROUP_CONTEXT_KEY);

	if (!value) {
		throw new Error(`${componentName} must be rendered inside SelectGroup.`);
	}

	return value;
}
