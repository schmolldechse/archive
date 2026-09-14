<script lang="ts">
	import { setToastProviderContext } from "./toast-context.svelte";
	import { createToastController, getToastControllerInternals } from "./toast-controller.svelte";
	import type { ToastProviderProps } from "./types";

	const localController = createToastController();

	let {
		controller = localController,
		limit = 3,
		defaultDuration = null,
		pauseWhenPageHidden = true,
		label = "Notifications",
		children
	}: ToastProviderProps = $props();

	let presentedIds = $state<string[]>([]);
	let pausedIds = $state<string[]>([]);
	const rootGetters = new Map<string, () => HTMLLIElement | null>();
	let focusOrigin: HTMLElement | null = null;

	const normalizedLimit = $derived.by(() => {
		if (!Number.isInteger(limit) || limit < 1) {
			throw new Error("ToastProvider limit must be an integer of at least one.");
		}
		return limit;
	});

	const normalizedDefaultDuration = $derived.by(() => {
		if (defaultDuration !== null && (!Number.isFinite(defaultDuration) || defaultDuration < 5000)) {
			throw new Error("ToastProvider defaultDuration must be null or a finite value of at least 5000 milliseconds.");
		}
		return defaultDuration;
	});

	const internals = $derived(getToastControllerInternals(controller));
	const visibleRecords = $derived.by(() => {
		const entries = internals.entries;
		const presentedExitingIds = new Set(
			entries
				.filter((entry) => entry.state === "exiting" && presentedIds.includes(entry.record.id))
				.map((entry) => entry.record.id)
		);
		const availableSlots = Math.max(0, normalizedLimit - presentedExitingIds.size);
		const nextVisibleIds = new Set(
			entries
				.filter((entry) => entry.state === "visible")
				.slice(0, availableSlots)
				.map((entry) => entry.record.id)
		);

		return entries
			.filter((entry) => presentedExitingIds.has(entry.record.id) || nextVisibleIds.has(entry.record.id))
			.map((entry) => entry.record);
	});

	function focusWithoutScrolling(element: HTMLElement): void {
		try {
			element.focus({ preventScroll: true });
		} catch {
			element.focus();
		}
	}

	setToastProviderContext({
		get controller() {
			return controller;
		},
		get label() {
			return label;
		},
		get pauseWhenPageHidden() {
			return pauseWhenPageHidden;
		},
		get paused() {
			return pausedIds.length > 0;
		},
		get visibleRecords() {
			return visibleRecords;
		},
		dismiss(id) {
			return controller.dismiss(id);
		},
		effectiveDuration(id) {
			const entry = internals.entries.find((candidate) => candidate.record.id === id);
			if (!entry) return null;
			if (entry.durationSpecified) return entry.record.duration;
			if (entry.record.action || entry.record.priority === "high") return null;
			return normalizedDefaultDuration;
		},
		finalizeDismiss(id) {
			pausedIds = pausedIds.filter((candidate) => candidate !== id);
			presentedIds = presentedIds.filter((candidate) => candidate !== id);
			rootGetters.delete(id);
			return internals.finalizeDismiss(id);
		},
		finalizeUnpresentedDismissals() {
			for (const entry of internals.entries) {
				if (entry.state === "exiting" && !presentedIds.includes(entry.record.id)) {
					internals.finalizeDismiss(entry.record.id);
				}
			}
		},
		getRoot(id) {
			const element = rootGetters.get(id)?.() ?? null;
			return element?.isConnected ? element : null;
		},
		getState(id) {
			return internals.entries.find((candidate) => candidate.record.id === id)?.state ?? "exiting";
		},
		isPaused(id) {
			return pausedIds.includes(id);
		},
		registerRoot(id, getElement) {
			rootGetters.set(id, getElement);
			if (!presentedIds.includes(id)) presentedIds = [...presentedIds, id];

			return () => {
				if (rootGetters.get(id) === getElement) rootGetters.delete(id);
				presentedIds = presentedIds.filter((candidate) => candidate !== id);
				pausedIds = pausedIds.filter((candidate) => candidate !== id);
			};
		},
		rememberFocusOrigin(element) {
			focusOrigin = element;
		},
		restoreFocus(id) {
			if (focusOrigin?.isConnected && !focusOrigin.closest("[data-toast-viewport]")) {
				focusWithoutScrolling(focusOrigin);
				focusOrigin = null;
				return;
			}

			const root = rootGetters.get(id)?.() ?? null;
			const candidates = Array.from(
				document.querySelectorAll<HTMLElement>(
					'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
				)
			).filter((element) => !element.closest("[data-toast-viewport]") && element.getClientRects().length > 0);
			const preceding = root
				? candidates.filter((element) => Boolean(element.compareDocumentPosition(root) & Node.DOCUMENT_POSITION_FOLLOWING))
				: [];
			const fallback = preceding.at(-1) ?? candidates[0];
			if (fallback) focusWithoutScrolling(fallback);
			focusOrigin = null;
		},
		setPaused(id, paused) {
			if (paused && !pausedIds.includes(id)) pausedIds = [...pausedIds, id];
			else if (!paused && pausedIds.includes(id)) pausedIds = pausedIds.filter((candidate) => candidate !== id);
		}
	});
</script>

{@render children(controller)}
