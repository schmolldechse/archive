import type { ProcessingStatus } from "$api";

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
