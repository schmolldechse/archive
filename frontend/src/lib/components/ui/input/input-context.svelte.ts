import { getContext, setContext } from "svelte";

import type { InputState } from "./types";

export interface InputContext {
	readonly controlId: string;
	readonly descriptionId: string;
	readonly disabled: boolean;
	readonly errorId: string;
	readonly hasDescription: boolean;
	readonly invalid: boolean;
	readonly required: boolean;
	readonly state: InputState;
}

const INPUT_CONTEXT_KEY = Symbol("ReusableUiInputContext");

export function setInputContext(value: InputContext): void {
	setContext(INPUT_CONTEXT_KEY, value);
}

export function getInputContext(componentName: string): InputContext {
	const value = getContext<InputContext>(INPUT_CONTEXT_KEY);

	if (!value) {
		throw new Error(`${componentName} must be rendered inside InputRoot.`);
	}

	return value;
}
