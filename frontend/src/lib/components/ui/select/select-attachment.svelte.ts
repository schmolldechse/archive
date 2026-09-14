import { untrack } from "svelte";
import type { Attachment } from "svelte/attachments";

import type { SelectAlign, SelectSide } from "./types";

interface SelectAttachmentOptions {
	getAlign(): SelectAlign;
	getAnchor(): HTMLElement | null;
	getCollisionPadding(): number;
	getMatchTriggerWidth(): boolean;
	getOpen(): boolean;
	getSide(): SelectSide;
	getSideOffset(): number;
	containsTarget(target: Node): boolean;
	onClosed(): void;
	onNativeClose(): void;
	onOpened(): void;
	onOutsidePointer(): void;
	reconcileItems(): void;
	setResolvedPosition(side: SelectSide, align: SelectAlign): void;
	synchronizeProgrammaticOpen(): void;
}

interface Coordinates {
	left: number;
	top: number;
}

function supportsPopover(element: HTMLDivElement): boolean {
	return typeof element.showPopover === "function" && typeof element.hidePopover === "function";
}

function isPopoverOpen(element: HTMLDivElement): boolean {
	try {
		return element.matches(":popover-open");
	} catch {
		return false;
	}
}

function clamp(value: number, minimum: number, maximum: number): number {
	return Math.min(Math.max(value, minimum), Math.max(minimum, maximum));
}

function coordinatesFor(
	anchor: DOMRect,
	content: DOMRect,
	side: SelectSide,
	align: SelectAlign,
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

function verticalOverflow(coordinates: Coordinates, content: DOMRect, side: SelectSide, padding: number): number {
	if (side === "top") return Math.max(0, padding - coordinates.top);
	return Math.max(0, coordinates.top + content.height + padding - window.innerHeight);
}

function positionContent(content: HTMLDivElement, options: SelectAttachmentOptions): void {
	const anchor = options.getAnchor();
	if (!anchor?.isConnected) return;

	const padding = Math.max(0, options.getCollisionPadding());
	const availableHeight = Math.max(0, window.innerHeight - padding * 2);
	content.style.maxHeight = `${Math.floor(availableHeight)}px`;
	if (options.getMatchTriggerWidth()) content.style.minWidth = `${Math.ceil(anchor.getBoundingClientRect().width)}px`;
	else content.style.removeProperty("min-width");

	const anchorBounds = anchor.getBoundingClientRect();
	const contentBounds = content.getBoundingClientRect();
	const preferredSide = options.getSide();
	const alternateSide: SelectSide = preferredSide === "bottom" ? "top" : "bottom";
	const align = options.getAlign();
	const sideOffset = options.getSideOffset();
	const direction = getComputedStyle(anchor).direction;

	let resolvedSide = preferredSide;
	let coordinates = coordinatesFor(anchorBounds, contentBounds, preferredSide, align, sideOffset, direction);
	const preferredOverflow = verticalOverflow(coordinates, contentBounds, preferredSide, padding);

	if (preferredOverflow > 0) {
		const alternateCoordinates = coordinatesFor(
			anchorBounds,
			contentBounds,
			alternateSide,
			align,
			sideOffset,
			direction
		);
		const alternateOverflow = verticalOverflow(alternateCoordinates, contentBounds, alternateSide, padding);

		if (alternateOverflow < preferredOverflow) {
			resolvedSide = alternateSide;
			coordinates = alternateCoordinates;
		}
	}

	const left = clamp(coordinates.left, padding, window.innerWidth - contentBounds.width - padding);
	const top = clamp(coordinates.top, padding, window.innerHeight - contentBounds.height - padding);
	content.style.left = `${Math.round(left)}px`;
	content.style.top = `${Math.round(top)}px`;
	options.setResolvedPosition(resolvedSide, align);
}

export function createSelectAttachment(options: SelectAttachmentOptions): Attachment<HTMLDivElement> {
	return (content) => {
		let visible = false;
		let sequence = 0;
		const nativePopover = supportsPopover(content);

		const update = () => {
			options.reconcileItems();
			positionContent(content, options);
		};

		const handleToggle = (event: Event) => {
			if ((event as ToggleEvent).newState === "closed" && options.getOpen()) options.onNativeClose();
		};

		content.addEventListener("toggle", handleToggle);

		$effect(() => {
			const shouldOpen = options.getOpen();
			untrack(options.synchronizeProgrammaticOpen);

			if (!shouldOpen) {
				sequence += 1;
				if (nativePopover && isPopoverOpen(content)) content.hidePopover();
				content.hidden = true;
				content.inert = true;

				if (visible) {
					visible = false;
					options.onClosed();
				}
				return;
			}

			content.hidden = false;
			content.inert = false;
			const newlyVisible = !visible;
			if (nativePopover && !isPopoverOpen(content)) content.showPopover();
			visible = true;
			update();

			const currentSequence = ++sequence;
			if (newlyVisible) {
				queueMicrotask(() => {
					if (currentSequence !== sequence || !options.getOpen()) return;
					if (!nativePopover || isPopoverOpen(content)) options.onOpened();
				});
			}

			const handlePointerDown = (event: PointerEvent) => {
				if (event.defaultPrevented || event.button !== 0) return;
				const target = event.target;
				if (!(target instanceof Node) || options.containsTarget(target)) return;
				options.onOutsidePointer();
			};

			const resizeObserver = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(update);
			resizeObserver?.observe(content);
			const anchor = options.getAnchor();
			if (anchor) resizeObserver?.observe(anchor);

			const mutationObserver =
				typeof MutationObserver === "undefined" ? null : new MutationObserver(() => queueMicrotask(update));
			mutationObserver?.observe(content, { childList: true, subtree: true, characterData: true });

			window.addEventListener("resize", update);
			window.addEventListener("scroll", update, true);
			document.addEventListener("pointerdown", handlePointerDown, true);

			return () => {
				resizeObserver?.disconnect();
				mutationObserver?.disconnect();
				window.removeEventListener("resize", update);
				window.removeEventListener("scroll", update, true);
				document.removeEventListener("pointerdown", handlePointerDown, true);
			};
		});

		queueMicrotask(options.reconcileItems);

		return () => {
			sequence += 1;
			content.removeEventListener("toggle", handleToggle);
			if (nativePopover && isPopoverOpen(content)) content.hidePopover();
			visible = false;
		};
	};
}
