import type { SelectItemValue } from "./types";

export function isPrintableKey(event: KeyboardEvent): boolean {
	return event.key.length === 1 && !event.altKey && !event.ctrlKey && !event.metaKey;
}

export function normalizeSelectText(value: string): string {
	return value.trim().replace(/\s+/g, " ").toLocaleLowerCase();
}

export function joinIds(...values: Array<string | null | undefined>): string | undefined {
	const ids = values.flatMap((value) => value?.trim().split(/\s+/) ?? []).filter(Boolean);
	return ids.length > 0 ? [...new Set(ids)].join(" ") : undefined;
}

export function stringArraysEqual(current: SelectItemValue[], next: SelectItemValue[]): boolean {
	return current.length === next.length && current.every((entry, index) => entry === next[index]);
}
