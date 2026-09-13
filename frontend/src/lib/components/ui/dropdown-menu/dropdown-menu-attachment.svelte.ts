import type { Attachment } from "svelte/attachments";
import { untrack } from "svelte";

import type { DropdownMenuAlign, DropdownMenuSide } from "./types";

interface DropdownMenuAttachmentOptions {
	getAlign(): DropdownMenuAlign;
	getAlignOffset(): number;
	getAnchor(): HTMLElement | null;
	getCollisionPadding(): number;
	getOpen(): boolean;
	getSide(): DropdownMenuSide;
	getSideOffset(): number;
	containsTarget?(target: Node): boolean;
	onClosed(): void;
	onNativeClose(): void;
	onOpened(): void;
	onOutsidePointer?(): void;
	setResolvedPosition(side: DropdownMenuSide, align: DropdownMenuAlign): void;
	synchronizeProgrammaticChange(): void;
}

interface Coordinates {
	left: number;
	top: number;
}

function isPopoverOpen(element: HTMLElement): boolean {
	try {
		return element.matches(":popover-open");
	} catch {
		return false;
	}
}

function oppositeSide(side: DropdownMenuSide): DropdownMenuSide {
	switch (side) {
		case "top":
			return "bottom";
		case "right":
			return "left";
		case "bottom":
			return "top";
		case "left":
			return "right";
	}
}

function getCoordinates(
	anchor: DOMRect,
	content: DOMRect,
	side: DropdownMenuSide,
	align: DropdownMenuAlign,
	sideOffset: number,
	alignOffset: number,
	direction: string
): Coordinates {
	let left = anchor.left;
	let top = anchor.top;

	if (side === "top" || side === "bottom") {
		if (align === "center") left = anchor.left + (anchor.width - content.width) / 2 + alignOffset;
		else if ((align === "start") === (direction !== "rtl")) left = anchor.left + alignOffset;
		else left = anchor.right - content.width - alignOffset;

		top = side === "bottom" ? anchor.bottom + sideOffset : anchor.top - content.height - sideOffset;
	} else {
		if (align === "center") top = anchor.top + (anchor.height - content.height) / 2 + alignOffset;
		else if (align === "start") top = anchor.top + alignOffset;
		else top = anchor.bottom - content.height - alignOffset;

		left = side === "right" ? anchor.right + sideOffset : anchor.left - content.width - sideOffset;
	}

	return { left, top };
}

function mainAxisOverflow(
	coordinates: Coordinates,
	content: DOMRect,
	side: DropdownMenuSide,
	padding: number
): number {
	if (side === "top") return Math.max(0, padding - coordinates.top);
	if (side === "bottom") return Math.max(0, coordinates.top + content.height + padding - window.innerHeight);
	if (side === "left") return Math.max(0, padding - coordinates.left);
	return Math.max(0, coordinates.left + content.width + padding - window.innerWidth);
}

function clamp(value: number, minimum: number, maximum: number): number {
	return Math.min(Math.max(value, minimum), Math.max(minimum, maximum));
}

function positionPopover(popover: HTMLDivElement, options: DropdownMenuAttachmentOptions): void {
	const anchor = options.getAnchor();
	if (!anchor?.isConnected) return;

	const anchorBounds = anchor.getBoundingClientRect();
	const contentBounds = popover.getBoundingClientRect();
	const preferredSide = options.getSide();
	const align = options.getAlign();
	const padding = Math.max(0, options.getCollisionPadding());
	const sideOffset = options.getSideOffset();
	const alignOffset = options.getAlignOffset();
	const direction = getComputedStyle(anchor).direction;

	let resolvedSide = preferredSide;
	let coordinates = getCoordinates(
		anchorBounds,
		contentBounds,
		resolvedSide,
		align,
		sideOffset,
		alignOffset,
		direction
	);
	const preferredOverflow = mainAxisOverflow(coordinates, contentBounds, resolvedSide, padding);

	if (preferredOverflow > 0) {
		const flippedSide = oppositeSide(resolvedSide);
		const flippedCoordinates = getCoordinates(
			anchorBounds,
			contentBounds,
			flippedSide,
			align,
			sideOffset,
			alignOffset,
			direction
		);
		const flippedOverflow = mainAxisOverflow(flippedCoordinates, contentBounds, flippedSide, padding);

		if (flippedOverflow < preferredOverflow) {
			resolvedSide = flippedSide;
			coordinates = flippedCoordinates;
		}
	}

	const left = clamp(coordinates.left, padding, window.innerWidth - contentBounds.width - padding);
	const top = clamp(coordinates.top, padding, window.innerHeight - contentBounds.height - padding);

	popover.style.left = `${Math.round(left)}px`;
	popover.style.top = `${Math.round(top)}px`;
	options.setResolvedPosition(resolvedSide, align);
}

export function createDropdownMenuAttachment(
	options: DropdownMenuAttachmentOptions
): Attachment<HTMLDivElement> {
	return (popover) => {
		let visible = false;
		let openSequence = 0;

		const handleToggle = (event: Event) => {
			const newState = (event as ToggleEvent).newState;
			if (newState === "closed" && options.getOpen()) options.onNativeClose();
		};

		popover.addEventListener("toggle", handleToggle);

		$effect(() => {
			const shouldOpen = options.getOpen();
			untrack(options.synchronizeProgrammaticChange);

			if (!shouldOpen) {
				openSequence += 1;
				if (isPopoverOpen(popover)) popover.hidePopover();
				if (visible) {
					visible = false;
					options.onClosed();
				}
				return;
			}

			const newlyVisible = !visible;
			if (!isPopoverOpen(popover)) popover.showPopover();
			visible = true;
			positionPopover(popover, options);

			const sequence = ++openSequence;
			if (newlyVisible) {
				queueMicrotask(() => {
					if (sequence === openSequence && options.getOpen() && isPopoverOpen(popover)) options.onOpened();
				});
			}

			const updatePosition = () => positionPopover(popover, options);
			const handlePointerDown = (event: PointerEvent) => {
				if (!options.onOutsidePointer || event.defaultPrevented || event.button !== 0) return;
				const target = event.target;
				if (!(target instanceof Node) || options.containsTarget?.(target)) return;
				options.onOutsidePointer();
			};
			const resizeObserver = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(updatePosition);
			resizeObserver?.observe(popover);
			const anchor = options.getAnchor();
			if (anchor) resizeObserver?.observe(anchor);

			window.addEventListener("resize", updatePosition);
			window.addEventListener("scroll", updatePosition, true);
			document.addEventListener("pointerdown", handlePointerDown, true);

			return () => {
				resizeObserver?.disconnect();
				window.removeEventListener("resize", updatePosition);
				window.removeEventListener("scroll", updatePosition, true);
				document.removeEventListener("pointerdown", handlePointerDown, true);
			};
		});

		return () => {
			openSequence += 1;
			popover.removeEventListener("toggle", handleToggle);
			if (isPopoverOpen(popover)) popover.hidePopover();
			visible = false;
		};
	};
}
