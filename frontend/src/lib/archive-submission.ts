import type { ArchiveResponse } from "./api/types.gen";
import { normalizeTags } from "./components/tags/tags";

export type ArchiveSource = "url" | "file";

export interface ArchiveDraft {
	source: ArchiveSource;
	sourceUrl: string;
	file: File | null;
	originalLink: string;
	title: string;
	description: string;
	tags: string[];
}

export type ArchiveFieldErrors = Partial<
	Record<"sourceUrl" | "file" | "originalLink" | "title" | "description" | "tags" | "form", string>
>;

export interface ArchiveSourceDetails {
	title: string;
	source: string;
}
export interface ArchiveSourceStore {
	getItem(key: string): string | null;
	setItem(key: string, value: string): void;
}

export function saveArchiveSource(storage: ArchiveSourceStore, id: string, details: ArchiveSourceDetails): void {
	storage.setItem(`archive-submission:${id}`, JSON.stringify(details));
}

export function loadArchiveSource(storage: ArchiveSourceStore, id: string): ArchiveSourceDetails | null {
	const saved = storage.getItem(`archive-submission:${id}`);
	if (!saved) return null;
	try {
		const value: unknown = JSON.parse(saved);
		return value &&
			typeof value === "object" &&
			"title" in value &&
			typeof value.title === "string" &&
			"source" in value &&
			typeof value.source === "string"
			? { title: value.title, source: value.source }
			: null;
	} catch {
		return null;
	}
}

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

function validWebUrl(value: string): boolean {
	try {
		return ["http:", "https:"].includes(new URL(value).protocol);
	} catch {
		return false;
	}
}

export function validateDraft(draft: ArchiveDraft): ArchiveFieldErrors {
	const errors: ArchiveFieldErrors = {};
	if (draft.source === "url") {
		const sourceUrl = draft.sourceUrl.trim();
		if (!validWebUrl(sourceUrl) || sourceUrl.length > 2_048)
			errors.sourceUrl = "Enter a valid HTTP or HTTPS URL (up to 2,048 characters).";
	} else {
		if (!draft.file) errors.file = "Choose an HTML, MHTML or Webarchive file.";
		else if (!/\.(html|mhtml|webarchive)$/i.test(draft.file.name)) errors.file = "Choose a .html, .mhtml or .webarchive file.";
		else if (draft.file.size === 0 || draft.file.size > 250_000_000) errors.file = "Choose a non-empty file up to 250 MB.";
		const originalLink = draft.originalLink.trim();
		if (!validWebUrl(originalLink) || originalLink.length > 2_048)
			errors.originalLink = "Enter the original HTTP or HTTPS page URL (up to 2,048 characters).";
	}
	if (!draft.title.trim()) errors.title = "Add a title for this snapshot.";
	else if (draft.title.trim().length > 500) errors.title = "Use no more than 500 characters for the title.";
	if (draft.description.trim().length > 10_000) errors.description = "Use no more than 10,000 characters for the description.";
	const tags = normalizeTags(draft.tags);
	if (tags.length > 30 || tags.some((tag) => tag.length > 100))
		errors.tags = "Use no more than 30 tags, each up to 100 characters.";
	return errors;
}

async function archiveResponse(response: Response): Promise<ArchiveResponse> {
	if (response.ok) return (await response.json()) as ArchiveResponse;
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
		originallink: "originalLink",
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

export async function createArchive(
	baseUrl: string,
	draft: ArchiveDraft,
	fetcher: typeof fetch = fetch
): Promise<ArchiveResponse> {
	const errors = validateDraft(draft);
	if (Object.keys(errors).length) throw new ArchiveHttpError(0, "Please correct the highlighted fields.", errors);
	const endpoint = `${baseUrl.replace(/\/$/, "")}/api/archive`;
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
		body.append("originalLink", draft.originalLink.trim());
		if (description) body.append("description", description);
		for (const tag of tags) body.append("tags", tag);
	}
	return archiveResponse(await fetcher(endpoint, { method: "POST", headers, body }));
}

export async function getArchive(baseUrl: string, id: string, fetcher: typeof fetch = fetch): Promise<ArchiveResponse> {
	return archiveResponse(await fetcher(`${baseUrl.replace(/\/$/, "")}/api/archive/${encodeURIComponent(id)}`));
}

export async function cancelArchive(baseUrl: string, id: string, fetcher: typeof fetch = fetch): Promise<ArchiveResponse> {
	return archiveResponse(
		await fetcher(`${baseUrl.replace(/\/$/, "")}/api/archive/${encodeURIComponent(id)}/cancel`, { method: "POST" })
	);
}
