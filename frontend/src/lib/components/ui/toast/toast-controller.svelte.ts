import type { ToastActionOptions, ToastController, ToastOptions, ToastRecord, ToastUpdate } from "./types";

export type ToastLifecycleState = "visible" | "exiting";

export interface ToastControllerEntry {
	readonly record: ToastRecord;
	readonly state: ToastLifecycleState;
	readonly durationSpecified: boolean;
}

export interface ToastControllerInternals {
	readonly entries: readonly ToastControllerEntry[];
	finalizeDismiss(id: string): boolean;
}

const controllerInternals = new WeakMap<ToastController, ToastControllerInternals>();

function validateDuration(duration: number | null | undefined): void {
	if (duration === undefined || duration === null) return;
	if (!Number.isFinite(duration) || duration < 5000) {
		throw new Error("Toast duration must be null or a finite value of at least 5000 milliseconds.");
	}
}

function normalizeAction(action: ToastActionOptions | undefined): ToastActionOptions | undefined {
	if (!action) return undefined;
	if (!action.label.trim()) throw new Error("Toast action labels must not be empty.");

	return Object.freeze({
		label: action.label,
		onAction: action.onAction,
		dismissOnAction: action.dismissOnAction
	});
}

function createRecord(options: ToastOptions, id: string, revision: number): ToastRecord {
	if (!options.title.trim()) throw new Error("Toast titles must not be empty.");
	validateDuration(options.duration);

	return Object.freeze({
		id,
		title: options.title,
		description: options.description,
		variant: options.variant ?? "neutral",
		priority: options.priority ?? "normal",
		duration: options.duration ?? null,
		action: normalizeAction(options.action),
		dismissible: options.dismissible ?? true,
		revision
	});
}

function createUpdatedRecord(record: ToastRecord, update: ToastUpdate): ToastRecord {
	const nextDuration = Object.hasOwn(update, "duration") ? update.duration : record.duration;
	validateDuration(nextDuration);

	const nextTitle = update.title ?? record.title;
	if (!nextTitle.trim()) throw new Error("Toast titles must not be empty.");

	return Object.freeze({
		id: record.id,
		title: nextTitle,
		description: Object.hasOwn(update, "description") ? update.description : record.description,
		variant: update.variant ?? record.variant,
		priority: update.priority ?? record.priority,
		duration: nextDuration ?? null,
		action: Object.hasOwn(update, "action") ? normalizeAction(update.action) : record.action,
		dismissible: update.dismissible ?? record.dismissible,
		revision: record.revision + 1
	});
}

function generateToastId(): string {
	if (typeof globalThis.crypto?.randomUUID === "function") return `toast-${globalThis.crypto.randomUUID()}`;
	return `toast-${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
}

export function createToastController(): ToastController {
	let entries = $state<ToastControllerEntry[]>([]);
	const toasts = $derived(entries.map((entry) => entry.record));

	const controller: ToastController = {
		get toasts() {
			return toasts;
		},
		add(options) {
			const id = options.id ?? generateToastId();
			const existingIndex = entries.findIndex((entry) => entry.record.id === id);

			if (existingIndex === -1) {
				entries = [
					...entries,
					{
						record: createRecord(options, id, 0),
						state: "visible",
						durationSpecified: Object.hasOwn(options, "duration")
					}
				];
				return id;
			}

			const existing = entries[existingIndex];
			if (!existing) return id;
			const next = [...entries];
			next[existingIndex] = {
				record: createRecord(options, id, existing.record.revision + 1),
				state: "visible",
				durationSpecified: Object.hasOwn(options, "duration")
			};
			entries = next;
			return id;
		},
		update(id, update) {
			const index = entries.findIndex((entry) => entry.record.id === id);
			const existing = entries[index];
			if (!existing) return false;

			const next = [...entries];
			next[index] = {
				record: createUpdatedRecord(existing.record, update),
				state: "visible",
				durationSpecified: Object.hasOwn(update, "duration") ? true : existing.durationSpecified
			};
			entries = next;
			return true;
		},
		dismiss(id) {
			const index = entries.findIndex((entry) => entry.record.id === id);
			const existing = entries[index];
			if (!existing) return false;
			if (existing.state === "exiting") return true;

			const next = [...entries];
			next[index] = { ...existing, state: "exiting" };
			entries = next;
			return true;
		},
		dismissAll() {
			entries = entries.map((entry) => (entry.state === "exiting" ? entry : { ...entry, state: "exiting" }));
		}
	};

	controllerInternals.set(controller, {
		get entries() {
			return entries;
		},
		finalizeDismiss(id) {
			const next = entries.filter((entry) => entry.record.id !== id);
			if (next.length === entries.length) return false;
			entries = next;
			return true;
		}
	});

	return controller;
}

export function getToastControllerInternals(controller: ToastController): ToastControllerInternals {
	const internals = controllerInternals.get(controller);
	if (!internals) {
		throw new Error("ToastProvider controller must be created with createToastController().");
	}

	return internals;
}
