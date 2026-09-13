import { getContext, setContext } from "svelte";

import type { AccordionHeadingLevel } from "./types";

interface AccordionItemRegistration {
	readonly value: string;
}

export interface AccordionRootContext {
	readonly disabled: boolean;
	readonly headingLevel: AccordionHeadingLevel;
	readonly keyboardNavigation: boolean;
	readonly loop: boolean;
	isCollapseDisabled(value: string): boolean;
	isOpen(value: string): boolean;
	navigate(event: KeyboardEvent): void;
	openFromSearch(value: string): void;
	registerItem(item: AccordionItemRegistration): () => void;
	toggle(value: string, disabled: boolean): void;
}

export interface AccordionItemContext {
	readonly contentId: string;
	readonly collapseDisabled: boolean;
	readonly disabled: boolean;
	readonly headingLevel: AccordionHeadingLevel;
	readonly open: boolean;
	readonly triggerId: string;
	readonly value: string;
	navigate(event: KeyboardEvent): void;
	openFromSearch(): void;
	registerContentId(getId: () => string): () => void;
	registerTriggerId(getId: () => string): () => void;
	toggle(triggerDisabled: boolean): void;
}

const ACCORDION_CONTEXT_KEY = Symbol("ReusableUiAccordionContext");
const ACCORDION_ITEM_CONTEXT_KEY = Symbol("ReusableUiAccordionItemContext");

export function setAccordionRootContext(value: AccordionRootContext): void {
	setContext(ACCORDION_CONTEXT_KEY, value);
}

export function getAccordionRootContext(): AccordionRootContext {
	const value = getContext<AccordionRootContext>(ACCORDION_CONTEXT_KEY);

	if (!value) {
		throw new Error("AccordionItem must be rendered inside AccordionRoot.");
	}

	return value;
}

export function setAccordionItemContext(value: AccordionItemContext): void {
	setContext(ACCORDION_ITEM_CONTEXT_KEY, value);
}

export function getAccordionItemContext(componentName: string): AccordionItemContext {
	const value = getContext<AccordionItemContext>(ACCORDION_ITEM_CONTEXT_KEY);

	if (!value) {
		throw new Error(`${componentName} must be rendered inside AccordionItem.`);
	}

	return value;
}
