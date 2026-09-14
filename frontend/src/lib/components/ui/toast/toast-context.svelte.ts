import { getContext, setContext } from "svelte";

import type { ToastLifecycleState } from "./toast-controller.svelte";
import type { ToastController, ToastRecord } from "./types";

type IdGetter = () => string;
type ElementGetter<T extends HTMLElement> = () => T | null;

export interface ToastProviderContext {
	readonly controller: ToastController;
	readonly label: string;
	readonly pauseWhenPageHidden: boolean;
	readonly visibleRecords: readonly ToastRecord[];
	readonly paused: boolean;
	dismiss(id: string): boolean;
	effectiveDuration(id: string): number | null;
	finalizeDismiss(id: string): boolean;
	finalizeUnpresentedDismissals(): void;
	getRoot(id: string): HTMLLIElement | null;
	getState(id: string): ToastLifecycleState;
	isPaused(id: string): boolean;
	registerRoot(id: string, getElement: ElementGetter<HTMLLIElement>): () => void;
	rememberFocusOrigin(element: HTMLElement | null): void;
	restoreFocus(id: string): void;
	setPaused(id: string, paused: boolean): void;
}

export interface ToastItemContext {
	readonly defaultDescriptionId: string;
	readonly defaultTitleId: string;
	readonly descriptionId: string | undefined;
	readonly duration: number | null;
	readonly paused: boolean;
	readonly record: ToastRecord;
	readonly rootId: string;
	readonly state: ToastLifecycleState;
	dismiss(): boolean;
	registerDescriptionId(getId: IdGetter): () => void;
	registerTitleId(getId: IdGetter): () => void;
}

export interface ToastRecordContext {
	readonly record: ToastRecord;
}

const TOAST_PROVIDER_CONTEXT_KEY = Symbol("ReusableUiToastProviderContext");
const TOAST_RECORD_CONTEXT_KEY = Symbol("ReusableUiToastRecordContext");
const TOAST_ITEM_CONTEXT_KEY = Symbol("ReusableUiToastItemContext");

export function setToastProviderContext(value: ToastProviderContext): void {
	setContext(TOAST_PROVIDER_CONTEXT_KEY, value);
}

export function getToastProviderContext(componentName: string): ToastProviderContext {
	const value = getContext<ToastProviderContext>(TOAST_PROVIDER_CONTEXT_KEY);
	if (!value) throw new Error(`${componentName} must be rendered inside ToastProvider.`);
	return value;
}

export function setToastRecordContext(value: ToastRecordContext): void {
	setContext(TOAST_RECORD_CONTEXT_KEY, value);
}

export function getToastRecordContext(componentName: string): ToastRecordContext {
	const value = getContext<ToastRecordContext>(TOAST_RECORD_CONTEXT_KEY);
	if (!value) throw new Error(`${componentName} must be rendered by ToastViewport.`);
	return value;
}

export function setToastItemContext(value: ToastItemContext): void {
	setContext(TOAST_ITEM_CONTEXT_KEY, value);
}

export function getToastItemContext(componentName: string): ToastItemContext {
	const value = getContext<ToastItemContext>(TOAST_ITEM_CONTEXT_KEY);
	if (!value) throw new Error(`${componentName} must be rendered inside ToastRoot.`);
	return value;
}
