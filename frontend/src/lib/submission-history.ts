import type { SnapshotHistoryItemResponse } from "$api";

const historyData = (value: unknown): Record<string, unknown> =>
	value !== null && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : {};

/** Formats only supported payload versions; new events remain readable by older clients. */
export function historyLabel(item: SnapshotHistoryItemResponse): string {
	const content = item.content;
	if (!content || content.version !== 1) return "Snapshot event";
	const data = historyData(content.data);
	switch (content.type) {
		case "snapshot.submitted":
			return "Snapshot submitted";
		case "snapshot.stored":
			return "Snapshot stored";
		case "snapshot.resubmitted":
			return "Review requested again";
		case "snapshot.processing_finished":
			return data.status === "CANCELLED"
				? "Capture cancelled"
				: data.status === "ABORTED"
					? "Capture interrupted"
					: "Capture could not be completed";
		case "snapshot.moderated":
			return data.status === "REJECTED"
				? "Snapshot rejected"
				: data.source === "AUTOMATIC_APPROVAL"
					? "Snapshot approved automatically"
					: data.source === "LEGACY_MIGRATION"
						? "Existing snapshot approved"
						: "Snapshot approved";
		default:
			return "Snapshot event";
	}
}

/** Reasons are plain text and are never rendered as HTML or raw JSON. */
export function historyReason(item: SnapshotHistoryItemResponse): string | null {
	const content = item.content;
	const reason = content?.version === 1 && content.type === "snapshot.moderated" ? historyData(content.data).reason : null;
	return typeof reason === "string" ? reason : null;
}
