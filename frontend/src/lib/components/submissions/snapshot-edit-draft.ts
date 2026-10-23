import type { SnapshotSubmissionDetailResponse } from "$api";

/** Editable snapshot fields shared between the detail header and its inline form. */
export interface SnapshotEditDraft {
	title: string;
	description: string;
	sourceUrl: string;
	tags: string[];
	createdByHandle: string;
}

/** Creates a fresh edit draft from the latest server representation. */
export function createSnapshotEditDraft(detail: SnapshotSubmissionDetailResponse): SnapshotEditDraft {
	return {
		title: detail.snapshot.title,
		description: detail.snapshot.description ?? "",
		sourceUrl: detail.snapshot.sourceUrl ?? "",
		tags: [...detail.snapshot.tags],
		createdByHandle: detail.snapshot.createdBy?.handle ?? ""
	};
}
