import { getContext, setContext } from "svelte";

import type { DialogOpenReason } from "./types";

type IdGetter = () => string;
type ElementGetter<T extends HTMLElement> = () => T | null;

export interface DialogContext {
	readonly contentId: string;
	readonly descriptionId: string | undefined;
	readonly modal: boolean;
	readonly open: boolean;
	readonly titleId: string | undefined;
	getTrigger(): HTMLButtonElement | null;
	registerContentId(getId: IdGetter): () => void;
	registerDescriptionId(getId: IdGetter): () => void;
	registerTitleId(getId: IdGetter): () => void;
	registerTrigger(getElement: ElementGetter<HTMLButtonElement>): () => void;
	requestOpenChange(open: boolean, reason: DialogOpenReason): void;
	setLastTrigger(element: HTMLButtonElement): void;
	synchronizeProgrammaticChange(): void;
}

const DIALOG_CONTEXT_KEY = Symbol("ReusableUiDialogContext");

export function setDialogContext(value: DialogContext): void {
	setContext(DIALOG_CONTEXT_KEY, value);
}

export function getDialogContext(componentName: string): DialogContext {
	const value = getContext<DialogContext>(DIALOG_CONTEXT_KEY);

	if (!value) {
		throw new Error(`${componentName} must be rendered inside DialogRoot.`);
	}

	return value;
}
