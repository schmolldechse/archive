import type { ListSnapshotsData } from "$api";
import { searchSnapshotIndex } from "$lib/snapshot-search.remote";
import { DateTime } from "luxon";
import type { PageLoad } from "./$types";

const explicitOffset = /(?:Z|[+-]\d{2}:\d{2})$/;
const pageSize = 24;

function readBoundary(value: string | null): string | undefined {
	if (!value || !explicitOffset.test(value)) return undefined;

	const parsed = DateTime.fromISO(value, { setZone: true });
	return parsed.isValid ? value : undefined;
}

function readPage(value: string | null): number {
	if (!value || !/^\d+$/.test(value)) return 1;

	const page = Number(value);
	return Number.isSafeInteger(page) && page > 0 ? page : 1;
}

export const load: PageLoad = async ({ url }) => {
	const query = url.searchParams.get("text")?.trim() ?? "";
	const rawFrom = url.searchParams.get("capturedFrom");
	const rawTo = url.searchParams.get("capturedUntil");
	const from = readBoundary(rawFrom);
	const to = readBoundary(rawTo);
	const page = readPage(url.searchParams.get("page"));
	const invalidBoundary = (rawFrom && !from) || (rawTo && !to);
	const invalidOrder =
		from && to
			? DateTime.fromISO(from, { setZone: true }).toMillis() >= DateTime.fromISO(to, { setZone: true }).toMillis()
			: false;
	const filterError = invalidBoundary
		? "The capture range contains an invalid offset timestamp."
		: invalidOrder
			? "The end of the capture range must be later than its start."
			: null;
	const searchQuery: NonNullable<ListSnapshotsData["query"]> = {
		text: query || undefined,
		capturedFrom: filterError ? undefined : from,
		capturedUntil: filterError ? undefined : to,
		page,
		pageSize
	};

	try {
		const results = await searchSnapshotIndex(searchQuery);
		return { query, from, to, page, pageSize, filterError, results, apiAvailable: true, loadError: null };
	} catch (error) {
		return {
			query,
			from,
			to,
			page,
			pageSize,
			filterError,
			results: null,
			apiAvailable: false,
			loadError: error instanceof Error ? error.message : "The archive index could not be loaded."
		};
	}
};
