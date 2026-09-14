import type { Attachment } from "svelte/attachments";

import type { ToastLifecycleState } from "./toast-controller.svelte";
import type { ToastPriority, ToastRecord } from "./types";

const EXIT_DURATION = 150;

interface ToastRootAttachmentOptions {
	finalizeDismiss(): void;
	getDuration(): number | null;
	getPauseWhenPageHidden(): boolean;
	getRevision(): number;
	getState(): ToastLifecycleState;
	onTimeout(): void;
	setPaused(paused: boolean): void;
}

interface ToastViewportAttachmentOptions {
	announce(priority: ToastPriority, message: string): void;
	finalizeUnpresentedDismissals(): void;
	focusNewest(): void;
	getHotkey(): string;
	getVisibleRecords(): readonly ToastRecord[];
}

function announcementText(record: ToastRecord): string {
	return [record.title, record.description, record.action?.label].filter(Boolean).join(". ");
}

function motionIsReduced(): boolean {
	return window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ?? false;
}

export function createToastRootAttachment(options: ToastRootAttachmentOptions): Attachment<HTMLLIElement> {
	return (element) => {
		let timeout: ReturnType<typeof setTimeout> | undefined;
		let remaining = 0;
		let startedAt = 0;
		let pointerPaused = false;
		let focusPaused = false;
		let pagePaused = false;
		let observedRevision = -1;
		let observedDuration: number | null | undefined;
		let observedState: ToastLifecycleState | undefined;
		let observedPauseWhenPageHidden: boolean | undefined;

		const clearTimer = () => {
			if (timeout) clearTimeout(timeout);
			timeout = undefined;
		};

		const isPaused = () => pointerPaused || focusPaused || pagePaused;

		const synchronizePaused = () => options.setPaused(options.getState() === "visible" && isPaused());

		const startTimer = () => {
			if (options.getState() !== "visible" || remaining <= 0 || isPaused()) return;

			clearTimer();
			startedAt = performance.now();
			timeout = setTimeout(() => {
				timeout = undefined;
				remaining = 0;
				options.onTimeout();
			}, remaining);
		};

		const pauseTimer = () => {
			if (timeout) remaining = Math.max(0, remaining - (performance.now() - startedAt));
			clearTimer();
			synchronizePaused();
		};

		const resumeTimer = () => {
			synchronizePaused();
			startTimer();
		};

		const handlePointerEnter = () => {
			pointerPaused = true;
			pauseTimer();
		};

		const handlePointerLeave = () => {
			pointerPaused = false;
			resumeTimer();
		};

		const handleFocusIn = () => {
			focusPaused = true;
			pauseTimer();
		};

		const handleFocusOut = () => {
			queueMicrotask(() => {
				focusPaused = element.contains(document.activeElement);
				if (focusPaused) pauseTimer();
				else resumeTimer();
			});
		};

		const handleVisibilityChange = () => {
			pagePaused = options.getPauseWhenPageHidden() && document.hidden;
			if (pagePaused) pauseTimer();
			else resumeTimer();
		};

		element.addEventListener("pointerenter", handlePointerEnter);
		element.addEventListener("pointerleave", handlePointerLeave);
		element.addEventListener("focusin", handleFocusIn);
		element.addEventListener("focusout", handleFocusOut);
		document.addEventListener("visibilitychange", handleVisibilityChange);

		$effect(() => {
			const revision = options.getRevision();
			const state = options.getState();
			const duration = options.getDuration();
			const pauseWhenPageHidden = options.getPauseWhenPageHidden();
			const recordChanged = revision !== observedRevision || duration !== observedDuration;
			const stateChanged = state !== observedState;
			const pausePolicyChanged = pauseWhenPageHidden !== observedPauseWhenPageHidden;

			observedRevision = revision;
			observedDuration = duration;
			observedState = state;
			observedPauseWhenPageHidden = pauseWhenPageHidden;

			if (state === "exiting") {
				if (!stateChanged) return;
				clearTimer();
				remaining = 0;
				options.setPaused(false);
				timeout = setTimeout(options.finalizeDismiss, motionIsReduced() ? 0 : EXIT_DURATION);
				return;
			}

			if (recordChanged || stateChanged) {
				clearTimer();
				remaining = duration ?? 0;
				pagePaused = pauseWhenPageHidden && document.hidden;
				synchronizePaused();
				startTimer();
				return;
			}

			if (pausePolicyChanged) {
				const nextPagePaused = pauseWhenPageHidden && document.hidden;
				if (nextPagePaused === pagePaused) return;
				pagePaused = nextPagePaused;
				if (pagePaused) pauseTimer();
				else resumeTimer();
			}
		});

		return () => {
			clearTimer();
			options.setPaused(false);
			element.removeEventListener("pointerenter", handlePointerEnter);
			element.removeEventListener("pointerleave", handlePointerLeave);
			element.removeEventListener("focusin", handleFocusIn);
			element.removeEventListener("focusout", handleFocusOut);
			document.removeEventListener("visibilitychange", handleVisibilityChange);
		};
	};
}

export function createToastViewportAttachment(options: ToastViewportAttachmentOptions): Attachment<HTMLOListElement> {
	return () => {
		const announced = new Map<string, { revision: number; text: string; priority: ToastPriority }>();

		const handleKeydown = (event: KeyboardEvent) => {
			if (event.defaultPrevented || event.key !== options.getHotkey()) return;
			event.preventDefault();
			options.focusNewest();
		};

		document.addEventListener("keydown", handleKeydown);

		$effect(() => {
			options.finalizeUnpresentedDismissals();
			const visibleRecords = options.getVisibleRecords();
			const visibleIds = new Set(visibleRecords.map((record) => record.id));
			const normalMessages: string[] = [];
			const highMessages: string[] = [];

			for (const record of visibleRecords) {
				const text = announcementText(record);
				const previous = announced.get(record.id);
				if (!previous || (previous.revision !== record.revision && previous.text !== text)) {
					if (record.priority === "high") highMessages.push(text);
					else normalMessages.push(text);
				}
				announced.set(record.id, { revision: record.revision, text, priority: record.priority });
			}

			for (const id of announced.keys()) {
				if (!visibleIds.has(id)) announced.delete(id);
			}
			if (normalMessages.length > 0) options.announce("normal", normalMessages.join(". "));
			if (highMessages.length > 0) options.announce("high", highMessages.join(". "));
		});

		return () => document.removeEventListener("keydown", handleKeydown);
	};
}
