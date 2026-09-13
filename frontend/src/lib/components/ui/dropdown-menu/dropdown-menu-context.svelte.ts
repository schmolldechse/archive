import { getContext, setContext } from "svelte";

import { isPrintableKey, normalizeMenuText } from "./helpers";
import type { DropdownMenuFocusIntent, MenuOpenReason } from "./types";

type ElementGetter<T extends HTMLElement> = () => T | null;
type IdGetter = () => string;

export interface RegisteredDropdownMenuItem {
	readonly id: string;
	readonly submenuId?: string;
	getDisabled(): boolean;
	getElement(): HTMLElement | null;
	getTextValue(): string;
	openSubmenu?(): void;
}

interface RegisteredDropdownSubmenu {
	readonly id: string;
	close(reason: MenuOpenReason): void;
	open(reason: MenuOpenReason, focusIntent?: DropdownMenuFocusIntent): void;
}

export interface DropdownMenuContentContext {
	readonly activeItemId: string | null;
	readonly isSubmenu: boolean;
	activateItem(id: string, focus?: boolean): void;
	closeSubmenus(reason: MenuOpenReason, exceptId?: string): void;
	destroy(): void;
	focusFirst(): void;
	focusLast(): void;
	handleKeydown(event: KeyboardEvent): void;
	openSubmenu(id: string, reason: MenuOpenReason, focusIntent?: DropdownMenuFocusIntent): void;
	registerItem(item: RegisteredDropdownMenuItem): () => void;
	registerSubmenu(submenu: RegisteredDropdownSubmenu): () => void;
	reset(): void;
}

export interface DropdownMenuRootContext {
	readonly contentId: string;
	readonly disabled: boolean;
	readonly loop: boolean;
	readonly open: boolean;
	readonly triggerId: string;
	readonly typeahead: boolean;
	closeSubmenus(reason: MenuOpenReason): void;
	consumeFocusIntent(): DropdownMenuFocusIntent | null;
	consumeRestoreFocus(): boolean;
	getContent(): HTMLDivElement | null;
	getTrigger(): HTMLButtonElement | null;
	registerContent(getElement: ElementGetter<HTMLDivElement>): () => void;
	registerContentId(getId: IdGetter): () => void;
	registerMenu(getMenu: () => DropdownMenuContentContext): () => void;
	registerSubmenu(submenu: RegisteredDropdownSubmenu): () => void;
	registerTrigger(getElement: ElementGetter<HTMLButtonElement>): () => void;
	registerTriggerId(getId: IdGetter): () => void;
	requestOpenChange(
		open: boolean,
		reason: MenuOpenReason,
		focusIntent?: DropdownMenuFocusIntent,
		restoreFocus?: boolean
	): void;
	restoreTriggerFocus(): void;
	setLastTrigger(element: HTMLButtonElement): void;
	synchronizeProgrammaticChange(): void;
}

export interface DropdownMenuSubContext {
	readonly contentId: string;
	readonly id: string;
	readonly open: boolean;
	readonly triggerId: string;
	consumeFocusIntent(): DropdownMenuFocusIntent | null;
	consumeRestoreFocus(): boolean;
	getContent(): HTMLDivElement | null;
	getTrigger(): HTMLButtonElement | null;
	registerContent(getElement: ElementGetter<HTMLDivElement>): () => void;
	registerContentId(getId: IdGetter): () => void;
	registerMenu(getMenu: () => DropdownMenuContentContext): () => void;
	registerTrigger(getElement: ElementGetter<HTMLButtonElement>): () => void;
	registerTriggerId(getId: IdGetter): () => void;
	requestOpenChange(
		open: boolean,
		reason: MenuOpenReason,
		focusIntent?: DropdownMenuFocusIntent,
		restoreFocus?: boolean
	): void;
	restoreTriggerFocus(): void;
	synchronizeProgrammaticChange(): void;
}

export interface DropdownMenuGroupContext {
	readonly labelId: string | undefined;
	registerLabelId(getId: IdGetter): () => void;
}

export interface DropdownMenuCheckboxGroupContext {
	readonly value: string[];
	commit(value: string[]): void;
	registerValue(getValue: () => string): () => void;
}

export interface DropdownMenuRadioGroupContext {
	readonly value: string;
	commit(value: string): void;
	registerValue(getValue: () => string): () => void;
}

interface ContentControllerOptions {
	root: DropdownMenuRootContext;
	isSubmenu: boolean;
	closeCurrent(reason: MenuOpenReason, restoreFocus: boolean): void;
}

const ROOT_CONTEXT_KEY = Symbol("ReusableUiDropdownMenuRootContext");
const CONTENT_CONTEXT_KEY = Symbol("ReusableUiDropdownMenuContentContext");
const SUB_CONTEXT_KEY = Symbol("ReusableUiDropdownMenuSubmenuContext");
const GROUP_CONTEXT_KEY = Symbol("ReusableUiDropdownMenuGroupContext");
const CHECKBOX_CONTEXT_KEY = Symbol("ReusableUiDropdownMenuCheckboxGroupContext");
const RADIO_CONTEXT_KEY = Symbol("ReusableUiDropdownMenuRadioGroupContext");

function focusElement(element: HTMLElement): void {
	try {
		element.focus({ preventScroll: true });
	} catch {
		element.focus();
	}

	element.scrollIntoView({ block: "nearest", inline: "nearest" });
}

function sortItemsByDocumentOrder(items: RegisteredDropdownMenuItem[]): RegisteredDropdownMenuItem[] {
	return [...items].sort((left, right) => {
		const leftElement = left.getElement();
		const rightElement = right.getElement();
		if (!leftElement || !rightElement || leftElement === rightElement) return 0;

		const position = leftElement.compareDocumentPosition(rightElement);
		return position & Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1;
	});
}

export function createDropdownMenuContentContext(options: ContentControllerOptions): DropdownMenuContentContext {
	let activeItemId = $state<string | null>(null);
	const items: RegisteredDropdownMenuItem[] = [];
	const submenus: RegisteredDropdownSubmenu[] = [];
	let typeaheadBuffer = "";
	let typeaheadTimer: ReturnType<typeof setTimeout> | undefined;

	const enabledItems = () => sortItemsByDocumentOrder(items).filter((item) => !item.getDisabled() && item.getElement());

	const closeSubmenus = (reason: MenuOpenReason, exceptId?: string) => {
		for (const submenu of submenus) {
			if (submenu.id !== exceptId) submenu.close(reason);
		}
	};

	const activateItem = (id: string, focus = false) => {
		const item = items.find((entry) => entry.id === id);
		if (!item || item.getDisabled()) return;

		activeItemId = item.id;
		closeSubmenus("pointer", item.submenuId);
		if (focus) {
			const element = item.getElement();
			if (element) focusElement(element);
		}
	};

	const focusAt = (index: number) => {
		const available = enabledItems();
		const item = available[index];
		if (item) activateItem(item.id, true);
	};

	const move = (offset: number) => {
		const available = enabledItems();
		if (available.length === 0) return;

		let index = available.findIndex((item) => item.id === activeItemId);
		if (index === -1) index = offset > 0 ? -1 : available.length;
		let nextIndex = index + offset;

		if (nextIndex < 0 || nextIndex >= available.length) {
			if (!options.root.loop) return;
			nextIndex = (nextIndex + available.length) % available.length;
		}

		focusAt(nextIndex);
	};

	const search = (key: string) => {
		if (!options.root.typeahead) return;

		if (typeaheadTimer) clearTimeout(typeaheadTimer);
		typeaheadBuffer += normalizeMenuText(key);
		typeaheadTimer = setTimeout(() => {
			typeaheadBuffer = "";
			typeaheadTimer = undefined;
		}, 500);

		const available = enabledItems();
		const currentIndex = available.findIndex((item) => item.id === activeItemId);
		const candidates = [...available.slice(currentIndex + 1), ...available.slice(0, currentIndex + 1)];
		const repeatedCharacter = typeaheadBuffer.length > 1 && new Set(typeaheadBuffer).size === 1;
		const query = repeatedCharacter ? typeaheadBuffer[0] : typeaheadBuffer;
		const match = candidates.find((item) => normalizeMenuText(item.getTextValue()).startsWith(query));
		if (match) activateItem(match.id, true);
	};

	const handleKeydown = (event: KeyboardEvent) => {
		if (event.defaultPrevented || options.root.disabled) return;

		const direction = getComputedStyle(event.currentTarget as Element).direction;
		const forwardKey = direction === "rtl" ? "ArrowLeft" : "ArrowRight";
		const backwardKey = direction === "rtl" ? "ArrowRight" : "ArrowLeft";

		if (event.key === "ArrowDown") {
			event.preventDefault();
			move(1);
			return;
		}

		if (event.key === "ArrowUp") {
			event.preventDefault();
			move(-1);
			return;
		}

		if (event.key === "Home") {
			event.preventDefault();
			focusAt(0);
			return;
		}

		if (event.key === "End") {
			event.preventDefault();
			focusAt(enabledItems().length - 1);
			return;
		}

		if (event.key === forwardKey) {
			const activeItem = items.find((item) => item.id === activeItemId);
			if (activeItem?.openSubmenu) {
				event.preventDefault();
				activeItem.openSubmenu();
			}
			return;
		}

		if (event.key === backwardKey && options.isSubmenu) {
			event.preventDefault();
			options.closeCurrent("keyboard", true);
			return;
		}

		if (event.key === "Escape") {
			event.preventDefault();
			if (options.isSubmenu) options.closeCurrent("escape", true);
			else options.root.requestOpenChange(false, "escape", undefined, true);
			return;
		}

		if (event.key === "Tab") {
			queueMicrotask(() => options.root.requestOpenChange(false, "tab"));
			return;
		}

		if (event.key === "Enter" || event.key === " ") {
			const activeItem = items.find((item) => item.id === activeItemId);
			const element = activeItem?.getElement();
			if (element) {
				event.preventDefault();
				element.click();
			}
			return;
		}

		if (isPrintableKey(event)) {
			event.preventDefault();
			search(event.key);
		}
	};

	return {
		get activeItemId() {
			return activeItemId;
		},
		get isSubmenu() {
			return options.isSubmenu;
		},
		activateItem,
		closeSubmenus,
		destroy() {
			if (typeaheadTimer) clearTimeout(typeaheadTimer);
		},
		focusFirst() {
			focusAt(0);
		},
		focusLast() {
			focusAt(enabledItems().length - 1);
		},
		handleKeydown,
		openSubmenu(id, reason, focusIntent) {
			const submenu = submenus.find((entry) => entry.id === id);
			if (!submenu) return;
			closeSubmenus(reason, id);
			submenu.open(reason, focusIntent);
		},
		registerItem(item) {
			if (items.some((entry) => entry.id === item.id)) {
				throw new Error(`Dropdown menu item IDs must be unique. Duplicate ID: "${item.id}".`);
			}

			items.push(item);
			return () => {
				const index = items.indexOf(item);
				if (index !== -1) items.splice(index, 1);
				if (activeItemId === item.id) activeItemId = null;
			};
		},
		registerSubmenu(submenu) {
			submenus.push(submenu);
			return () => {
				const index = submenus.indexOf(submenu);
				if (index !== -1) submenus.splice(index, 1);
			};
		},
		reset() {
			activeItemId = null;
			closeSubmenus("native");
			typeaheadBuffer = "";
			if (typeaheadTimer) clearTimeout(typeaheadTimer);
			typeaheadTimer = undefined;
		}
	};
}

export function setDropdownMenuRootContext(value: DropdownMenuRootContext): void {
	setContext(ROOT_CONTEXT_KEY, value);
}

export function getDropdownMenuRootContext(componentName: string): DropdownMenuRootContext {
	const value = getContext<DropdownMenuRootContext>(ROOT_CONTEXT_KEY);
	if (!value) throw new Error(`${componentName} must be rendered inside DropdownMenuRoot.`);
	return value;
}

export function setDropdownMenuContentContext(value: DropdownMenuContentContext): void {
	setContext(CONTENT_CONTEXT_KEY, value);
}

export function getDropdownMenuContentContext(componentName: string): DropdownMenuContentContext {
	const value = getContext<DropdownMenuContentContext>(CONTENT_CONTEXT_KEY);
	if (!value) throw new Error(`${componentName} must be rendered inside DropdownMenuContent or DropdownMenuSubContent.`);
	return value;
}

export function setDropdownMenuSubContext(value: DropdownMenuSubContext): void {
	setContext(SUB_CONTEXT_KEY, value);
}

export function getDropdownMenuSubContext(componentName: string): DropdownMenuSubContext {
	const value = getContext<DropdownMenuSubContext>(SUB_CONTEXT_KEY);
	if (!value) throw new Error(`${componentName} must be rendered inside DropdownMenuSub.`);
	return value;
}

export function setDropdownMenuGroupContext(value: DropdownMenuGroupContext): void {
	setContext(GROUP_CONTEXT_KEY, value);
}

export function getDropdownMenuGroupContext(componentName: string): DropdownMenuGroupContext {
	const value = getContext<DropdownMenuGroupContext>(GROUP_CONTEXT_KEY);
	if (!value) throw new Error(`${componentName} must be rendered inside DropdownMenuGroup.`);
	return value;
}

export function setDropdownMenuCheckboxGroupContext(value: DropdownMenuCheckboxGroupContext): void {
	setContext(CHECKBOX_CONTEXT_KEY, value);
}

export function getDropdownMenuCheckboxGroupContext(componentName: string): DropdownMenuCheckboxGroupContext {
	const value = getContext<DropdownMenuCheckboxGroupContext>(CHECKBOX_CONTEXT_KEY);
	if (!value) throw new Error(`${componentName} must be rendered inside DropdownMenuCheckboxGroup.`);
	return value;
}

export function setDropdownMenuRadioGroupContext(value: DropdownMenuRadioGroupContext): void {
	setContext(RADIO_CONTEXT_KEY, value);
}

export function getDropdownMenuRadioGroupContext(componentName: string): DropdownMenuRadioGroupContext {
	const value = getContext<DropdownMenuRadioGroupContext>(RADIO_CONTEXT_KEY);
	if (!value) throw new Error(`${componentName} must be rendered inside DropdownMenuRadioGroup.`);
	return value;
}
