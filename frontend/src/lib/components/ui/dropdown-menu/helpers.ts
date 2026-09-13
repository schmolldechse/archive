import type { DropdownMenuSelectEvent } from "./types";

export function createDropdownMenuSelectEvent(originalEvent: Event): DropdownMenuSelectEvent {
	let defaultPrevented = false;

	return {
		get defaultPrevented() {
			return defaultPrevented;
		},
		originalEvent,
		preventDefault() {
			defaultPrevented = true;
		}
	};
}

export function normalizeMenuText(value: string): string {
	return value.trim().replace(/\s+/g, " ").toLocaleLowerCase();
}

export function isPrintableKey(event: KeyboardEvent): boolean {
	return event.key.length === 1 && !event.altKey && !event.ctrlKey && !event.metaKey;
}

export function stringArraysEqual(current: string[], next: string[]): boolean {
	return current.length === next.length && current.every((entry, index) => entry === next[index]);
}
