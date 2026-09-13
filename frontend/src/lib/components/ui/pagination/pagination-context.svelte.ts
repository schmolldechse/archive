import { getContext, setContext } from "svelte";

import type { PaginationMode, PaginationPageState } from "./types";

export interface PaginationContext {
	readonly disabled: boolean;
	readonly mode: PaginationMode;
	readonly page: number;
	readonly pageCount: number;
	getHref(page: number): string;
	getPageState(page: number): PaginationPageState;
	isPageUnavailable(page: number): boolean;
	select(page: number, event: MouseEvent): void;
}

const PAGINATION_CONTEXT_KEY = Symbol("ReusableUiPaginationContext");

export function setPaginationContext(value: PaginationContext): void {
	setContext(PAGINATION_CONTEXT_KEY, value);
}

export function getPaginationContext(componentName: string): PaginationContext {
	const value = getContext<PaginationContext>(PAGINATION_CONTEXT_KEY);

	if (!value) {
		throw new Error(`${componentName} must be rendered inside PaginationRoot.`);
	}

	return value;
}
