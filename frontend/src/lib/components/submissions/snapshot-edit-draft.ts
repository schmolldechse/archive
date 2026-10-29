import type { SnapshotSubmissionDetailResponse } from "$api";

/** Editable snapshot fields shared by the owner and administrator edit routes. */
export interface SnapshotEditDraft {
	title: string;
	description: string;
	sourceUrl: string;
	tags: string[];
	createdByHandle: string;
	isPrivate: boolean;
}

/** Creates a fresh edit draft from the latest server representation. */
export function createSnapshotEditDraft(detail: SnapshotSubmissionDetailResponse): SnapshotEditDraft {
	return {
		title: detail.snapshot.title,
		description: detail.snapshot.description ?? "",
		sourceUrl: detail.snapshot.sourceUrl ?? "",
		tags: [...detail.snapshot.tags],
		createdByHandle: detail.snapshot.createdBy?.handle ?? "",
		isPrivate: detail.snapshot.isPrivate
	};
}
