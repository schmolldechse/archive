import type { ClassValue } from "svelte/elements";

const TABBABLE_SELECTOR = [
	"a[href]",
	"area[href]",
	"button:not([disabled])",
	'input:not([disabled]):not([type="hidden"])',
	"select:not([disabled])",
	"textarea:not([disabled])",
	"details > summary:first-of-type",
	'[contenteditable="true"]',
	'[tabindex]:not([tabindex="-1"])'
].join(",");

export function mergeClasses(...classes: Array<ClassValue | null | undefined>): ClassValue {
	return classes.filter(Boolean) as ClassValue;
}

export function getTabsValueIdSegment(value: string): string {
	if (value.length === 0) return "empty";

	return Array.from(value, (character) => character.codePointAt(0)?.toString(36) ?? "").join("-");
}

export function isFirstMeaningfulContentTabbable(element: HTMLElement): boolean {
	const firstMeaningfulNode = Array.from(element.childNodes).find((node) => {
		if (node.nodeType === Node.TEXT_NODE) return Boolean(node.textContent?.trim());
		if (!(node instanceof Element) || node.matches("script, style, template")) return false;
		if (node.closest('[hidden], [inert], [aria-hidden="true"]')) return false;

		const styles = getComputedStyle(node);
		return styles.display !== "none" && styles.visibility !== "hidden" && styles.visibility !== "collapse";
	});

	return (
		firstMeaningfulNode instanceof HTMLElement &&
		firstMeaningfulNode.matches(TABBABLE_SELECTOR) &&
		firstMeaningfulNode.tabIndex >= 0
	);
}
