import type { Attachment } from "svelte/attachments";

import type { DialogAlign, DialogOpenReason, DialogPosition, DialogSide } from "./types";

interface DialogAttachmentOptions {
	getAlign(): DialogAlign;
	getCollisionPadding(): number;
	getCloseOnEscape(): boolean;
	getCloseOnOutsidePointer(modal: boolean): boolean;
	getInitialFocus(): (() => HTMLElement | null) | undefined;
	getModal(): boolean;
	getOpen(): boolean;
	getPosition(): DialogPosition;
	getPreventScroll(modal: boolean): boolean;
	getRestoreFocus(): boolean;
	getReturnFocus(): (() => HTMLElement | null) | undefined;
	getSide(): DialogSide;
	getSideOffset(): number;
	getTitleId(): string | undefined;
	getTrigger(): HTMLButtonElement | null;
	requestOpenChange(open: boolean, reason: DialogOpenReason): void;
	setEffectiveModal(modal: boolean): void;
	setResolvedSide(side: DialogSide): void;
	synchronizeProgrammaticChange(): void;
}

interface Coordinates {
	left: number;
	top: number;
}

let scrollLockCount = 0;
let previousDocumentOverflow = "";
const openDialogs: HTMLDialogElement[] = [];

const FOCUSABLE_SELECTOR = [
	"a[href]",
	"area[href]",
	"button:not([disabled])",
	'input:not([disabled]):not([type="hidden"])',
	"select:not([disabled])",
	"textarea:not([disabled])",
	"iframe",
	"object",
	"embed",
	'[contenteditable="true"]',
	'[tabindex]:not([tabindex="-1"])'
].join(",");

function isAvailableFocusTarget(element: HTMLElement | null): element is HTMLElement {
	if (!element?.isConnected) return false;
	if (element.closest("[inert]")) return false;
	if (element.getAttribute("aria-hidden") === "true") return false;
	if (element instanceof HTMLButtonElement && element.disabled) return false;
	if (element instanceof HTMLInputElement && element.disabled) return false;
	if (element instanceof HTMLSelectElement && element.disabled) return false;
	if (element instanceof HTMLTextAreaElement && element.disabled) return false;
	if (element.hidden) return false;

	return element.getClientRects().length > 0;
}

function getFocusableElements(dialog: HTMLDialogElement): HTMLElement[] {
	return Array.from(dialog.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)).filter(isAvailableFocusTarget);
}

function focusWithoutScrolling(element: HTMLElement): void {
	try {
		element.focus({ preventScroll: true });
	} catch {
		element.focus();
	}
}

function assertAccessibleName(dialog: HTMLDialogElement): void {
	const ariaLabel = dialog.getAttribute("aria-label")?.trim();
	const ariaLabelledby = dialog.getAttribute("aria-labelledby")?.trim();

	if (ariaLabel || ariaLabelledby) return;

	throw new Error("DialogContent requires DialogTitle or an explicit aria-label.");
}

function focusDialogFallback(dialog: HTMLDialogElement, titleId: string | undefined): void {
	const title = titleId ? document.getElementById(titleId) : null;

	if (title instanceof HTMLElement && dialog.contains(title)) {
		focusWithoutScrolling(title);
		return;
	}

	const previousTabIndex = dialog.getAttribute("tabindex");
	dialog.setAttribute("tabindex", "-1");
	focusWithoutScrolling(dialog);

	if (previousTabIndex === null) dialog.removeAttribute("tabindex");
	else dialog.setAttribute("tabindex", previousTabIndex);
}

function focusInitialTarget(dialog: HTMLDialogElement, options: DialogAttachmentOptions, modal: boolean): void {
	const initialFocus = options.getInitialFocus();
	const requestedTarget = initialFocus?.() ?? null;

	if (isAvailableFocusTarget(requestedTarget) && dialog.contains(requestedTarget)) {
		focusWithoutScrolling(requestedTarget);
		return;
	}

	if (!modal) return;

	const autofocusTarget = dialog.querySelector<HTMLElement>("[autofocus]");
	if (isAvailableFocusTarget(autofocusTarget)) {
		focusWithoutScrolling(autofocusTarget);
		return;
	}

	const firstFocusable = getFocusableElements(dialog)[0];
	if (firstFocusable) {
		focusWithoutScrolling(firstFocusable);
		return;
	}

	focusDialogFallback(dialog, options.getTitleId());
}

function lockDocumentScroll(): void {
	if (scrollLockCount === 0) {
		previousDocumentOverflow = document.documentElement.style.overflow;
		document.documentElement.style.overflow = "hidden";
	}

	scrollLockCount += 1;
}

function unlockDocumentScroll(): void {
	if (scrollLockCount === 0) return;

	scrollLockCount -= 1;

	if (scrollLockCount === 0) {
		document.documentElement.style.overflow = previousDocumentOverflow;
		previousDocumentOverflow = "";
	}
}

function registerOpenDialog(dialog: HTMLDialogElement): void {
	const existingIndex = openDialogs.indexOf(dialog);
	if (existingIndex !== -1) openDialogs.splice(existingIndex, 1);
	openDialogs.push(dialog);
}

function unregisterOpenDialog(dialog: HTMLDialogElement): void {
	const index = openDialogs.indexOf(dialog);
	if (index !== -1) openDialogs.splice(index, 1);
}

function isTopmostDialog(dialog: HTMLDialogElement): boolean {
	return openDialogs.at(-1) === dialog;
}

function isPointerInsideDialog(dialog: HTMLDialogElement, event: PointerEvent): boolean {
	const target = event.target;

	if (!(target instanceof Node) || !dialog.contains(target)) return false;
	if (target !== dialog) return true;

	const bounds = dialog.getBoundingClientRect();
	return (
		event.clientX >= bounds.left &&
		event.clientX <= bounds.right &&
		event.clientY >= bounds.top &&
		event.clientY <= bounds.bottom
	);
}

function clamp(value: number, minimum: number, maximum: number): number {
	return Math.min(Math.max(value, minimum), Math.max(minimum, maximum));
}

function anchoredCoordinates(
	anchor: DOMRect,
	content: DOMRect,
	side: DialogSide,
	align: DialogAlign,
	sideOffset: number,
	direction: string
): Coordinates {
	let left: number;

	if (align === "center") left = anchor.left + (anchor.width - content.width) / 2;
	else if ((align === "start") === (direction !== "rtl")) left = anchor.left;
	else left = anchor.right - content.width;

	const top = side === "bottom" ? anchor.bottom + sideOffset : anchor.top - content.height - sideOffset;
	return { left, top };
}

function verticalOverflow(coordinates: Coordinates, content: DOMRect, side: DialogSide, padding: number): number {
	if (side === "top") return Math.max(0, padding - coordinates.top);
	return Math.max(0, coordinates.top + content.height + padding - window.innerHeight);
}

function positionAtTrigger(dialog: HTMLDialogElement, options: DialogAttachmentOptions): void {
	const trigger = options.getTrigger();
	if (!trigger?.isConnected || !dialog.open) return;

	dialog.style.removeProperty("max-height");
	dialog.style.removeProperty("overflow-y");
	const padding = Math.max(0, options.getCollisionPadding());
	const triggerBounds = trigger.getBoundingClientRect();
	let dialogBounds = dialog.getBoundingClientRect();
	const preferredSide = options.getSide();
	const alternateSide: DialogSide = preferredSide === "bottom" ? "top" : "bottom";
	const align = options.getAlign();
	const sideOffset = options.getSideOffset();
	const direction = getComputedStyle(trigger).direction;

	let resolvedSide = preferredSide;
	let coordinates = anchoredCoordinates(triggerBounds, dialogBounds, preferredSide, align, sideOffset, direction);
	const preferredOverflow = verticalOverflow(coordinates, dialogBounds, preferredSide, padding);

	if (preferredOverflow > 0) {
		const alternateCoordinates = anchoredCoordinates(triggerBounds, dialogBounds, alternateSide, align, sideOffset, direction);
		const alternateOverflow = verticalOverflow(alternateCoordinates, dialogBounds, alternateSide, padding);

		if (alternateOverflow < preferredOverflow) {
			resolvedSide = alternateSide;
			coordinates = alternateCoordinates;
		}
	}

	const availableHeight = Math.max(
		0,
		resolvedSide === "bottom"
			? window.innerHeight - triggerBounds.bottom - sideOffset - padding
			: triggerBounds.top - sideOffset - padding
	);
	if (dialogBounds.height > availableHeight) {
		dialog.style.maxHeight = `${Math.floor(availableHeight)}px`;
		dialog.style.overflowY = "auto";
		dialogBounds = dialog.getBoundingClientRect();
		coordinates = anchoredCoordinates(triggerBounds, dialogBounds, resolvedSide, align, sideOffset, direction);
	}

	dialog.style.left = `${Math.round(clamp(coordinates.left, padding, window.innerWidth - dialogBounds.width - padding))}px`;
	dialog.style.top = `${Math.round(coordinates.top)}px`;
	options.setResolvedSide(resolvedSide);
}

function restoreFocusTarget(options: DialogAttachmentOptions, focusBeforeOpen: HTMLElement | null): void {
	const explicitTarget = options.getReturnFocus()?.() ?? null;
	const target = [explicitTarget, options.getTrigger(), focusBeforeOpen].find(isAvailableFocusTarget);

	if (target) {
		focusWithoutScrolling(target);
		return;
	}

	const body = document.body;
	const previousTabIndex = body.getAttribute("tabindex");
	body.setAttribute("tabindex", "-1");
	focusWithoutScrolling(body);

	if (previousTabIndex === null) body.removeAttribute("tabindex");
	else body.setAttribute("tabindex", previousTabIndex);
}

export function createDialogAttachment(options: DialogAttachmentOptions): Attachment<HTMLDialogElement> {
	return (dialog) => {
		let openedAsModal: boolean | null = null;
		let focusBeforeOpen: HTMLElement | null = null;
		let scrollLocked = false;
		let modeWarningShown = false;
		let focusRequest = 0;
		let restoreFocusAfterClose = true;

		const releaseScrollLock = () => {
			if (!scrollLocked) return;
			scrollLocked = false;
			unlockDocumentScroll();
		};

		const handleCancel = (event: Event) => {
			if (event.defaultPrevented || !isTopmostDialog(dialog)) return;

			event.preventDefault();
			if (options.getCloseOnEscape()) {
				restoreFocusAfterClose = true;
				options.requestOpenChange(false, "escape");
			}
		};

		const handleClose = () => {
			if (options.getOpen()) {
				restoreFocusAfterClose = true;
				options.requestOpenChange(false, "native");
			}
		};

		const handlePointerDown = (event: PointerEvent) => {
			if (event.defaultPrevented || event.button !== 0 || !options.getOpen() || !isTopmostDialog(dialog)) return;

			const modal = openedAsModal ?? options.getModal();
			if (!options.getCloseOnOutsidePointer(modal) || isPointerInsideDialog(dialog, event)) return;

			restoreFocusAfterClose = modal;
			options.requestOpenChange(false, "outside");
		};

		const handleKeyDown = (event: KeyboardEvent) => {
			if (event.defaultPrevented || !options.getOpen() || !isTopmostDialog(dialog)) return;

			if (event.key === "Escape") {
				event.preventDefault();
				if (options.getCloseOnEscape()) {
					restoreFocusAfterClose = true;
					options.requestOpenChange(false, "escape");
				}
				return;
			}

			if (event.key !== "Tab" || openedAsModal !== true) return;

			const focusable = getFocusableElements(dialog);
			if (focusable.length === 0) {
				event.preventDefault();
				focusDialogFallback(dialog, options.getTitleId());
				return;
			}

			const activeElement = document.activeElement;
			const first = focusable[0];
			const last = focusable.at(-1);

			if (event.shiftKey && (activeElement === first || !dialog.contains(activeElement))) {
				event.preventDefault();
				if (last) focusWithoutScrolling(last);
			} else if (!event.shiftKey && (activeElement === last || !dialog.contains(activeElement))) {
				event.preventDefault();
				focusWithoutScrolling(first);
			}
		};

		dialog.addEventListener("cancel", handleCancel);
		dialog.addEventListener("close", handleClose);
		document.addEventListener("pointerdown", handlePointerDown);
		document.addEventListener("keydown", handleKeyDown);

		$effect(() => {
			options.synchronizeProgrammaticChange();

			const shouldOpen = options.getOpen();
			const requestedModal = options.getModal();

			if (shouldOpen) {
				if (openedAsModal === null) {
					openedAsModal = requestedModal;
					registerOpenDialog(dialog);
					modeWarningShown = false;
					restoreFocusAfterClose = true;
					focusBeforeOpen = document.activeElement instanceof HTMLElement ? document.activeElement : null;
				} else if (openedAsModal !== requestedModal && !modeWarningShown) {
					modeWarningShown = true;
					if (import.meta.env.DEV) {
						console.warn("DialogRoot modal cannot change while the dialog is open. Close and reopen it to change modes.");
					}
				}

				options.setEffectiveModal(openedAsModal);

				if (!dialog.open) {
					if (openedAsModal) dialog.showModal();
					else dialog.show();

					const currentFocusRequest = ++focusRequest;
					queueMicrotask(() => {
						if (currentFocusRequest !== focusRequest || !dialog.open || !options.getOpen()) return;
						assertAccessibleName(dialog);
						focusInitialTarget(dialog, options, openedAsModal === true);
					});
				}

				const shouldLockScroll = options.getPreventScroll(openedAsModal);
				if (shouldLockScroll && !scrollLocked) {
					lockDocumentScroll();
					scrollLocked = true;
				} else if (!shouldLockScroll) {
					releaseScrollLock();
				}

				if (openedAsModal || options.getPosition() !== "trigger") return;

				const updatePosition = () => positionAtTrigger(dialog, options);
				updatePosition();
				const frame = requestAnimationFrame(updatePosition);
				const resizeObserver = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(updatePosition);
				resizeObserver?.observe(dialog);
				const trigger = options.getTrigger();
				if (trigger) resizeObserver?.observe(trigger);
				window.addEventListener("resize", updatePosition);
				window.addEventListener("scroll", updatePosition, true);

				return () => {
					cancelAnimationFrame(frame);
					resizeObserver?.disconnect();
					window.removeEventListener("resize", updatePosition);
					window.removeEventListener("scroll", updatePosition, true);
				};
			}

			focusRequest += 1;
			if (dialog.open) dialog.close();
			releaseScrollLock();

			if (openedAsModal !== null && options.getRestoreFocus() && restoreFocusAfterClose) {
				restoreFocusTarget(options, focusBeforeOpen);
			}

			unregisterOpenDialog(dialog);
			openedAsModal = null;
			focusBeforeOpen = null;
			modeWarningShown = false;
			restoreFocusAfterClose = true;
			options.setEffectiveModal(requestedModal);
		});

		return () => {
			focusRequest += 1;
			dialog.removeEventListener("cancel", handleCancel);
			dialog.removeEventListener("close", handleClose);
			document.removeEventListener("pointerdown", handlePointerDown);
			document.removeEventListener("keydown", handleKeyDown);
			releaseScrollLock();

			if (dialog.open) dialog.close();
			unregisterOpenDialog(dialog);
			if (openedAsModal !== null && options.getRestoreFocus()) {
				restoreFocusTarget(options, focusBeforeOpen);
			}
			openedAsModal = null;
			focusBeforeOpen = null;
		};
	};
}
