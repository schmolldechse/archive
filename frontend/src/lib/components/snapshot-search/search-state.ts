import { SnapshotOrder, SnapshotQuality, SourceType, type CaptureDistributionResponse, type ListSnapshotsData } from "$api";
import { DateTime } from "luxon";

export type CaptureOrder = "asc" | "desc";
export interface SearchDraft {
	query: string;
	title: string;
	tags: string[];
	sourceType: SourceType | "";
	quality: SnapshotQuality | "";
	from: string;
	through: string;
	order: CaptureOrder;
}
export interface SearchState {
	query: string;
	title: string;
	tags: string[];
	sourceType?: SourceType;
	quality?: SnapshotQuality;
	capturedFrom?: string;
	capturedUntil?: string;
	order: CaptureOrder;
	page: number;
}
export type SearchErrors = Partial<Record<keyof SearchDraft | "form", string>>;
export interface FilterChip {
	field: keyof SearchDraft;
	label: string;
}
export interface CaptureSegment {
	firstYear: number;
	lastYear: number;
	count: number;
	label: string;
	from: string;
	through: string;
	position: number;
	size: number;
}
export const pageSize = 24;
export const emptyDraft = (): SearchDraft => ({
	query: "",
	title: "",
	tags: [],
	sourceType: "",
	quality: "",
	from: "",
	through: "",
	order: "desc"
});
export const emptyState = (): SearchState => ({
	query: "",
	title: "",
	tags: [],
	order: "desc",
	page: 1
});

export function parseDate(value: string): DateTime | null {
	if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return null;
	const date = DateTime.fromISO(value);
	return date.isValid && date.year >= 1 && date.toISODate() === value ? date : null;
}
function boundaryDate(value: string | undefined, inclusiveEnd = false): string {
	if (!value) return "";
	const date = DateTime.fromISO(value, { setZone: true });
	if (!date.isValid) return value;
	return (inclusiveEnd ? date.minus({ milliseconds: 1 }) : date).toISODate() ?? "";
}
export function draftFromState(state: SearchState): SearchDraft {
	return {
		query: state.query,
		title: state.title,
		tags: [...state.tags],
		sourceType: state.sourceType ?? "",
		quality: state.quality ?? "",
		from: boundaryDate(state.capturedFrom),
		through: boundaryDate(state.capturedUntil, true),
		order: state.order
	};
}
export function normalizeTags(value: string | readonly string[]): string[] {
	const tags = typeof value === "string" ? value.split(",") : value;

	return [...new Set(tags.map((tag) => tag.trim().replace(/^#+/, "").toLowerCase()).filter(Boolean))];
}
export function validateDraft(draft: SearchDraft): SearchErrors {
	const errors: SearchErrors = {};
	const query = draft.query.trim();
	if (query) {
		try {
			const source = new URL(query);
			if (!["http:", "https:"].includes(source.protocol) || query.length > 2048) throw new Error();
		} catch {
			errors.query = "Enter a complete HTTP or HTTPS URL, for example https://example.org/page (at most 2,048 characters).";
		}
	}
	if (draft.title.trim().length > 500) errors.title = "Use at most 500 characters.";
	const tags = normalizeTags(draft.tags);
	if (tags.length > 30) errors.tags = "Require at most 30 different tags.";
	else if (tags.some((tag) => tag.length > 100)) errors.tags = "Each tag must contain at most 100 characters.";
	if (draft.sourceType && !Object.values(SourceType).includes(draft.sourceType))
		errors.sourceType = "Choose a valid source type.";
	if (draft.quality && !Object.values(SnapshotQuality).includes(draft.quality))
		errors.quality = "Choose a valid capture quality.";
	if (draft.order !== "asc" && draft.order !== "desc") errors.order = "Choose a valid capture order.";
	const from = parseDate(draft.from),
		through = parseDate(draft.through);
	if (draft.from && !from) errors.from = "Choose a valid start date from the calendar.";
	if (draft.through && !through) errors.through = "Choose a valid end date from the calendar.";
	if (from && through && from.toMillis() > through.toMillis())
		errors.through = "The end date must be on or after the start date.";
	return errors;
}
export function stateFromDraft(draft: SearchDraft, previous: SearchState = emptyState()): SearchState {
	const old = draftFromState(previous);
	return {
		query: draft.query.trim(),
		title: draft.title.trim(),
		tags: normalizeTags(draft.tags),
		sourceType: draft.sourceType || undefined,
		quality: draft.quality || undefined,
		capturedFrom:
			draft.from === old.from ? previous.capturedFrom : (parseDate(draft.from)?.startOf("day").toISO() ?? undefined),
		capturedUntil:
			draft.through === old.through
				? previous.capturedUntil
				: (parseDate(draft.through)?.plus({ days: 1 }).startOf("day").toISO() ?? undefined),
		order: draft.order,
		page: 1
	};
}
export function apiQuery(state: SearchState): NonNullable<ListSnapshotsData["query"]> {
	return {
		sourceUrl: state.query || undefined,
		title: state.title || undefined,
		tags: state.tags.length ? state.tags : undefined,
		sourceType: state.sourceType,
		quality: state.quality,
		capturedFrom: state.capturedFrom,
		capturedUntil: state.capturedUntil,
		order: state.order === "asc" ? SnapshotOrder.ASC : SnapshotOrder.DESC,
		page: state.page,
		pageSize
	};
}
export function searchHref(state: SearchState, anchor = ""): string {
	const parameters = new URLSearchParams(),
		query = apiQuery(state);
	for (const key of [
		"sourceUrl",
		"title",
		"tags",
		"sourceType",
		"quality",
		"capturedFrom",
		"capturedUntil",
		"order",
		"page"
	] as const) {
		const value = query[key];
		if (value === undefined || (key === "order" && value === "desc") || (key === "page" && value === 1)) continue;
		if (Array.isArray(value)) value.forEach((tag) => parameters.append(key, tag));
		else parameters.set(key, String(value));
	}
	return `/${parameters.size ? `?${parameters}` : ""}${anchor ? `#${anchor}` : ""}`;
}
export function readSearchState(parameters: URLSearchParams): { state: SearchState; errors: SearchErrors } {
	const errors: SearchErrors = {};
	const read = (name: string, field: keyof SearchErrors): string => {
		if (parameters.getAll(name).length > 1) errors[field] = `Provide ${name} only once.`;
		return parameters.get(name)?.trim() ?? "";
	};
	const state: SearchState = {
		query: read("sourceUrl", "query"),
		title: read("title", "title"),
		tags: [
			...new Set(
				parameters
					.getAll("tags")
					.map((tag) => tag.trim().replace(/^#+/, "").toLowerCase())
					.filter(Boolean)
			)
		],
		sourceType: (read("sourceType", "sourceType") || undefined) as SourceType | undefined,
		quality: (read("quality", "quality") || undefined) as SnapshotQuality | undefined,
		capturedFrom: read("capturedFrom", "from") || undefined,
		capturedUntil: read("capturedUntil", "through") || undefined,
		order: "desc",
		page: 1
	};
	const order = read("order", "order");
	if (order && order !== "asc" && order !== "desc") errors.order = "Capture order must be asc or desc.";
	else if (order === "asc" || order === "desc") state.order = order;
	const page = Number(read("page", "form"));
	if (Number.isSafeInteger(page) && page >= 1 && page <= 1_000_000) state.page = page;
	for (const [value, field] of [
		[state.capturedFrom, "from"],
		[state.capturedUntil, "through"]
	] as const) {
		if (value && (!/(?:Z|[+-]\d{2}:\d{2})$/.test(value) || !DateTime.fromISO(value, { setZone: true }).isValid))
			errors[field] = "The link contains an invalid offset timestamp. Choose a date to correct it.";
	}
	const draftErrors = validateDraft(draftFromState(state));
	if (
		state.capturedFrom &&
		state.capturedUntil &&
		!errors.from &&
		!errors.through &&
		DateTime.fromISO(state.capturedFrom).toMillis() >= DateTime.fromISO(state.capturedUntil).toMillis()
	)
		errors.through = "The end of the capture range must be later than its start.";
	return { state, errors: { ...draftErrors, ...errors } };
}
export function isDraftDirty(draft: SearchDraft, state: SearchState): boolean {
	const normalize = (value: SearchDraft) => ({
		...value,
		query: value.query.trim(),
		title: value.title.trim(),
		tags: normalizeTags(value.tags).sort()
	});
	return JSON.stringify(normalize(draft)) !== JSON.stringify(normalize(draftFromState(state)));
}
export function filterChips(state: SearchState): FilterChip[] {
	const draft = draftFromState(state),
		chips: FilterChip[] = [];
	if (state.query) chips.push({ field: "query", label: `Source: ${state.query}` });
	if (state.title) chips.push({ field: "title", label: `Title: ${state.title}` });
	if (state.tags.length) chips.push({ field: "tags", label: `Tags: ${state.tags.join(" + ")}` });
	if (state.sourceType) chips.push({ field: "sourceType", label: state.sourceType === "URL" ? "Web URL" : "Uploaded HTML" });
	if (state.quality) chips.push({ field: "quality", label: state.quality === "COMPLETE" ? "Complete" : "Incomplete" });
	if (draft.from) chips.push({ field: "from", label: `From ${draft.from}` });
	if (draft.through) chips.push({ field: "through", label: `Through ${draft.through}` });
	if (state.order === "asc") chips.push({ field: "order", label: "Oldest first" });
	return chips;
}
export function captureSegments(distribution: CaptureDistributionResponse | null): CaptureSegment[] {
	if (!distribution || distribution.firstYear === null || distribution.lastYear === null) return [];
	const first = Number(distribution.firstYear),
		span = Number(distribution.lastYear) - first + 1,
		length = Math.min(span, 5);
	const years = new Map(distribution.years.map((year) => [Number(year.year), Number(year.count)]));
	let start = first;
	const segments: CaptureSegment[] = [];
	for (let index = 0; index < length; index++) {
		const size = Math.floor(span / length) + (index < span % length ? 1 : 0),
			end = start + size - 1;
		let count = 0;
		for (let year = start; year <= end; year++) count += years.get(year) ?? 0;
		segments.push({
			firstYear: start,
			lastYear: end,
			count,
			label: start === end ? String(start) : `${start}–${end}`,
			from: `${String(start).padStart(4, "0")}-01-01`,
			through: `${String(end).padStart(4, "0")}-12-31`,
			position: 4 + ((start - first + size / 2) / span) * 92,
			size: 8
		});
		start = end + 1;
	}
	const maximum = Math.max(...segments.map((segment) => segment.count), 1);
	return segments.map((segment) => ({ ...segment, size: 8 + (5 * Math.log1p(segment.count)) / Math.log1p(maximum) }));
}
