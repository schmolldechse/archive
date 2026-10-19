import type { ProcessingStatus, SnapshotQuality } from "$api";

export type CaptureBadgeVariant = "info" | "success" | "warning" | "critical";

/** Keeps technical capture outcomes distinct from the independent review status. */
const labels: Record<ProcessingStatus, string> = {
	QUEUED: "Waiting for capture",
	RUNNING: "Capture in progress",
	COMPLETED: "Capture complete",
	FAILED: "Capture failed",
	CANCELLED: "Capture cancelled",
	DISCARDED: "Capture discarded",
	ABORTED: "Capture interrupted"
};

export function processingLabel(status: ProcessingStatus): string {
	return labels[status] ?? "Capture status unavailable";
}

/** Summarizes processing and stored quality as one concise capture state. */
export function captureLabel(status: ProcessingStatus, quality: SnapshotQuality | null): string {
	if (status !== "COMPLETED") return processingLabel(status);
	if (quality === "COMPLETE") return "Fully captured";
	if (quality === "INCOMPLETE") return "Incomplete capture";
	return "Capture stored";
}

/** Keeps capture success, partial results, and active or failed work visually distinct. */
export function captureVariant(status: ProcessingStatus, quality: SnapshotQuality | null): CaptureBadgeVariant {
	if (status !== "COMPLETED") return status === "FAILED" || status === "ABORTED" ? "critical" : "info";
	return quality === "COMPLETE" ? "success" : "warning";
}
