import type { Component } from "svelte";
import type { PublicUserResponse, SnapshotHistoryItemResponse } from "$api";
import HistoryAttributionUpdated from "./events/HistoryAttributionUpdated.svelte";
import HistoryCapture from "./events/HistoryCapture.svelte";
import HistoryDetailsUpdated from "./events/HistoryDetailsUpdated.svelte";
import HistoryModerated from "./events/HistoryModerated.svelte";
import HistorySourceUpdated from "./events/HistorySourceUpdated.svelte";
import HistorySummary from "./events/HistorySummary.svelte";

export type HistoryTone = "neutral" | "register" | "success" | "warning" | "critical";
export type HistoryEventComponent = Component<{ item: SnapshotHistoryItemResponse }>;

interface HistoryPresentation {
	category: string;
	title: string;
	tone: HistoryTone;
}

interface HistoryEventDefinition {
	component: HistoryEventComponent;
	presentation: (data: Record<string, unknown>) => HistoryPresentation;
}

/** Narrows the deliberately open JSON history payload without trusting its shape. */
export function historyData(item: SnapshotHistoryItemResponse): Record<string, unknown> {
	const value = item.content.data;
	return value !== null && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : {};
}

export function historyString(data: Record<string, unknown>, key: string): string | null {
	return typeof data[key] === "string" ? data[key] : null;
}

/** Narrows one nested event value to a plain object before reading field-level changes. */
export function historyObject(data: Record<string, unknown>, key: string): Record<string, unknown> | null {
	const value = data[key];
	return value !== null && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

export function historyStrings(data: Record<string, unknown>, key: string): string[] {
	const value = data[key];
	return Array.isArray(value) ? value.filter((entry): entry is string => typeof entry === "string") : [];
}

export function historyBoolean(data: Record<string, unknown>, key: string): boolean {
	return data[key] === true;
}

export function historyNumber(data: Record<string, unknown>, key: string): number | null {
	const value = data[key];
	return typeof value === "number" && Number.isFinite(value) ? value : null;
}

export function historyUser(data: Record<string, unknown>, key: string): PublicUserResponse | null {
	const value = data[key];
	if (value === null || typeof value !== "object" || Array.isArray(value)) return null;
	const user = value as Record<string, unknown>;
	if (typeof user.handle !== "string" || typeof user.displayName !== "string" || typeof user.role !== "string") return null;
	return value as PublicUserResponse;
}

const fallbackPresentation: HistoryPresentation = { category: "Record", title: "Snapshot event", tone: "neutral" };

/**
 * Registers renderers and labels by the complete event contract key. A new payload version therefore
 * cannot accidentally inherit assumptions made for an older shape.
 */
const eventDefinitions: Record<string, HistoryEventDefinition> = {
	"snapshot.submitted@1": {
		component: HistorySummary,
		presentation: () => ({ category: "Submission", title: "Snapshot submitted", tone: "register" })
	},
	"snapshot.resubmitted@1": {
		component: HistorySummary,
		presentation: () => ({ category: "Submission", title: "Review requested again", tone: "register" })
	},
	"snapshot.stored@1": {
		component: HistoryCapture,
		presentation: () => ({ category: "Capture", title: "Snapshot stored", tone: "success" })
	},
	"snapshot.processing_finished@1": {
		component: HistorySummary,
		presentation: () => ({ category: "Capture", title: "Capture processing finished", tone: "neutral" })
	},
	"snapshot.processing_finished@2": {
		component: HistoryCapture,
		presentation: (data) => {
			const status = historyString(data, "status");
			return {
				category: "Capture",
				title:
					status === "CANCELLED"
						? "Capture cancelled"
						: status === "ABORTED"
							? "Capture interrupted"
							: status === "DISCARDED"
								? "Capture discarded"
								: "Capture failed",
				tone: status === "CANCELLED" ? "neutral" : "warning"
			};
		}
	},
	"snapshot.moderated@1": {
		component: HistoryModerated,
		presentation: (data) => {
			const status = historyString(data, "status");
			return {
				category: "Moderation",
				title: status === "REJECTED" ? "Snapshot rejected" : "Snapshot approved",
				tone: status === "REJECTED" ? "critical" : "success"
			};
		}
	},
	"snapshot.details_updated@1": {
		component: HistoryDetailsUpdated,
		presentation: () => ({ category: "Revision", title: "Snapshot details updated", tone: "register" })
	},
	"snapshot.details_updated@2": {
		component: HistoryDetailsUpdated,
		presentation: () => ({ category: "Revision", title: "Snapshot details updated", tone: "register" })
	},
	"snapshot.source_updated@1": {
		component: HistorySourceUpdated,
		presentation: (data) => ({
			category: "Provenance",
			title: "Source URL updated",
			tone: historyBoolean(data, "reviewRequired") ? "warning" : "register"
		})
	},
	"snapshot.attribution_updated@1": {
		component: HistoryAttributionUpdated,
		presentation: () => ({ category: "Attribution", title: "Snapshot attribution updated", tone: "register" })
	}
};

function historyEventDefinition(item: SnapshotHistoryItemResponse): HistoryEventDefinition | undefined {
	return eventDefinitions[`${item.content.type}@${item.content.version}`];
}

/** Returns an event-specific renderer while preserving a readable fallback for future contracts. */
export function historyEventComponent(item: SnapshotHistoryItemResponse): HistoryEventComponent {
	return historyEventDefinition(item)?.component ?? HistorySummary;
}

/** Supplies stable labels and restrained semantic tones independently from event-specific details. */
export function historyPresentation(item: SnapshotHistoryItemResponse): HistoryPresentation {
	const definition = historyEventDefinition(item);
	return definition ? definition.presentation(historyData(item)) : fallbackPresentation;
}
