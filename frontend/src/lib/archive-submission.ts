import type { SnapshotCreationResponse, SnapshotResponse, SnapshotProcessingResponse } from "./api/types.gen";
import { normalizeTags } from "./components/tags/tags";
import { snapshotLimits, validSourceUrl } from "./snapshot-validation";

export type ArchiveSource = "url" | "file";

export interface ArchiveDraft {
	source: ArchiveSource;
	sourceUrl: string;
	file: File | null;
	title: string;
	description: string;
	tags: string[];
}

export type ArchiveFieldErrors = Partial<Record<"sourceUrl" | "file" | "title" | "description" | "tags" | "form", string>>;

export class ArchiveHttpError extends Error {
	constructor(
		public status: number,
		message: string,
		public fieldErrors: ArchiveFieldErrors = {}
	) {
		super(message);
		this.name = "ArchiveHttpError";
	}
}

export function validateDraft(draft: ArchiveDraft): ArchiveFieldErrors {
	const errors: ArchiveFieldErrors = {};
	if (!validSourceUrl(draft.sourceUrl))
		errors.sourceUrl = "Enter a valid HTTP or HTTPS URL (up to 4,096 characters after normalization).";
	if (draft.source === "file") {
		if (!draft.file) errors.file = "Choose an HTML, MHTML or Webarchive file.";
		else if (!/\.(html|mhtml|webarchive)$/i.test(draft.file.name)) errors.file = "Choose a .html, .mhtml or .webarchive file.";
		else if (draft.file.size === 0 || draft.file.size > 250_000_000) errors.file = "Choose a non-empty file up to 250 MB.";
	}
	if (!draft.title.trim()) errors.title = "Add a title for this snapshot.";
	else if (draft.title.length > snapshotLimits.title) errors.title = "Use no more than 512 characters for the title.";
	if (draft.description.length > snapshotLimits.description)
		errors.description = "Use no more than 2,048 characters for the description.";
	const tags = normalizeTags(draft.tags);
	if (tags.length > snapshotLimits.tags || tags.some((tag) => tag.length > snapshotLimits.tag))
		errors.tags = "Use no more than 30 tags, each up to 128 characters.";
	return errors;
}

async function apiResponse<T>(response: Response): Promise<T> {
	if (response.ok) return (await response.json()) as T;
	let problem: unknown;
	try {
		const body = await response.text();
		try {
			problem = JSON.parse(body);
		} catch {
			problem = body.trim();
		}
	} catch {
		problem = null;
	}
	const details = problem && typeof problem === "object" ? (problem as Record<string, unknown>) : {};
	const fieldErrors: ArchiveFieldErrors = {};
	const fields: Record<string, keyof ArchiveFieldErrors> = {
		sourceurl: "sourceUrl",
		file: "file",
		title: "title",
		description: "description",
		tags: "tags"
	};
	if (details.errors && typeof details.errors === "object") {
		for (const [field, messages] of Object.entries(details.errors)) {
			const key = fields[field.split(".").pop()!.toLowerCase()];
			if (key && Array.isArray(messages))
				fieldErrors[key] = messages.filter((message): message is string => typeof message === "string").join(" ");
		}
	}
	const message =
		response.status < 500 && typeof problem === "string" && problem
			? problem
			: typeof details.detail === "string"
				? details.detail
				: typeof details.title === "string"
					? details.title
					: response.status === 413
						? "The selected file exceeds the archive size limit."
						: response.status === 409
							? "This archive can no longer be cancelled."
							: response.status === 404
								? "This archive could not be found."
								: response.status >= 500
									? "The archive service is temporarily unavailable. Please try again."
									: "The archive request could not be completed. Please check the entered information.";
	throw new ArchiveHttpError(response.status, message, fieldErrors);
}

export async function createSnapshot(
	baseUrl: string,
	draft: ArchiveDraft,
	fetcher: typeof fetch = fetch
): Promise<SnapshotCreationResponse> {
	const errors = validateDraft(draft);
	if (Object.keys(errors).length) throw new ArchiveHttpError(0, "Please correct the highlighted fields.", errors);
	const endpoint = `${baseUrl.replace(/\/$/, "")}/api/snapshots`;
	const title = draft.title.trim();
	const description = draft.description.trim();
	const tags = normalizeTags(draft.tags);
	let body: string | FormData;
	let headers: HeadersInit | undefined;
	if (draft.source === "url") {
		body = JSON.stringify({ sourceUrl: draft.sourceUrl.trim(), title, ...(description ? { description } : {}), tags });
		headers = { "content-type": "application/json" };
	} else {
		body = new FormData();
		body.append("file", draft.file!);
		body.append("title", title);
		body.append("sourceUrl", draft.sourceUrl.trim());
		if (description) body.append("description", description);
		for (const tag of tags) body.append("tags", tag);
	}
	return apiResponse(await fetcher(endpoint, { method: "POST", headers, body, credentials: "include" }));
}

export async function getSnapshotStatus(
	baseUrl: string,
	id: string,
	fetcher: typeof fetch = fetch
): Promise<SnapshotProcessingResponse> {
	return apiResponse(await fetcher(`${baseUrl.replace(/\/$/, "")}/api/snapshots/${encodeURIComponent(id)}/status`));
}

export async function cancelSnapshot(
	baseUrl: string,
	id: string,
	fetcher: typeof fetch = fetch
): Promise<SnapshotProcessingResponse> {
	return apiResponse(
		await fetcher(`${baseUrl.replace(/\/$/, "")}/api/snapshots/${encodeURIComponent(id)}/cancel`, { method: "POST" })
	);
}

export async function getSnapshot(baseUrl: string, id: string, fetcher: typeof fetch = fetch): Promise<SnapshotResponse> {
	return apiResponse(await fetcher(`${baseUrl.replace(/\/$/, "")}/api/snapshots/${encodeURIComponent(id)}`));
}
