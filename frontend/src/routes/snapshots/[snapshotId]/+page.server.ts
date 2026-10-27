import { error } from "@sveltejs/kit";
import { backendOrigin } from "$lib/server/api";
import type {
	SnapshotHistoryResponse,
	SnapshotPageTreeResponse,
	SnapshotResponse,
	SnapshotSubmissionDetailResponse
} from "$api";
import type { PageServerLoad } from "./$types";

const snapshotIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

interface ResolvedPrivateSnapshot {
	snapshot: SnapshotResponse;
	historyPath: string;
	pageTreePath: string;
}

/** Reads private metadata only through existing owner or moderator authorization. */
async function readPrivateSnapshot(
	fetcher: typeof fetch,
	apiBaseUrl: string,
	cookie: string,
	scope: "admin" | "users",
	snapshotId: string
): Promise<ResolvedPrivateSnapshot | null> {
	let response: Response;
	try {
		response = await fetcher(`${apiBaseUrl}/api/${scope}/snapshots/${snapshotId}`, {
			headers: { cookie, accept: "application/json" },
			cache: "no-store",
			redirect: "error",
			signal: AbortSignal.timeout(10000)
		});
	} catch {
		error(502, "The archived page is temporarily unavailable.");
	}
	if (response.status === 401 || response.status === 403 || response.status === 404) return null;
	if (!response.ok) error(502, "The archived page is temporarily unavailable.");

	const detail = (await response.json()) as SnapshotSubmissionDetailResponse;
	return detail.snapshot.contentUrl
		? {
				snapshot: detail.snapshot,
				historyPath: `/api/${scope}/snapshots/${snapshotId}/history`,
				pageTreePath: `/api/${scope}/snapshots/${snapshotId}/page-tree`
			}
		: null;
}

/** Loads the newest visible history events through the same audience boundary as the snapshot. */
async function readHistory(
	fetcher: typeof fetch,
	apiBaseUrl: string,
	path: string,
	cookie: string
): Promise<SnapshotHistoryResponse | null> {
	let response: Response;
	try {
		response = await fetcher(`${apiBaseUrl}${path}?page=1&pageSize=100`, {
			headers: cookie ? { cookie, accept: "application/json" } : { accept: "application/json" },
			cache: "no-store",
			redirect: "error",
			signal: AbortSignal.timeout(10000)
		});
	} catch {
		return null;
	}
	if (!response.ok) return null;
	try {
		return (await response.json()) as SnapshotHistoryResponse;
	} catch {
		return null;
	}
}

/** Loads page-tree metadata through the already selected public or private visibility scope. */
async function readPageTree(
	fetcher: typeof fetch,
	apiBaseUrl: string,
	path: string,
	cookie: string
): Promise<SnapshotPageTreeResponse | null> {
	try {
		const response = await fetcher(`${apiBaseUrl}${path}`, {
			headers: cookie ? { cookie, accept: "application/json" } : { accept: "application/json" },
			cache: "no-store",
			redirect: "error",
			signal: AbortSignal.timeout(10000)
		});
		if (!response.ok) return null;
		return (await response.json()) as SnapshotPageTreeResponse;
	} catch {
		return null;
	}
}

export const load: PageServerLoad = async ({ params, url, fetch, request, setHeaders }) => {
	if (!snapshotIdPattern.test(params.snapshotId)) error(404, "Snapshot not found");

	const apiBaseUrl = backendOrigin();
	let response: Response;
	try {
		response = await fetch(`${apiBaseUrl}/api/snapshots/${params.snapshotId}`, {
			cache: "no-store",
			redirect: "error",
			signal: AbortSignal.timeout(10000)
		});
	} catch {
		error(502, "The archived page is temporarily unavailable.");
	}

	let snapshot: SnapshotResponse;
	let isPrivate = false;
	let historyPath = `/api/snapshots/${params.snapshotId}/history`;
	let pageTreePath = `/api/snapshots/${params.snapshotId}/page-tree`;
	const cookie = request.headers.get("cookie") ?? "";
	if (response.status === 404) {
		const authorizedSnapshot =
			(await readPrivateSnapshot(fetch, apiBaseUrl, cookie, "admin", params.snapshotId)) ??
			(await readPrivateSnapshot(fetch, apiBaseUrl, cookie, "users", params.snapshotId));
		if (!authorizedSnapshot) error(404, "Snapshot not found");
		snapshot = authorizedSnapshot.snapshot;
		historyPath = authorizedSnapshot.historyPath;
		pageTreePath = authorizedSnapshot.pageTreePath;
		isPrivate = true;
		setHeaders({ "cache-control": "private, no-store" });
	} else {
		if (!response.ok) error(502, "The archived page is temporarily unavailable.");
		snapshot = (await response.json()) as SnapshotResponse;
	}
	// History is supplementary metadata: a transient failure must not hide an otherwise readable capture.
	const [loadedHistory, pageTree] = await Promise.all([
		readHistory(fetch, apiBaseUrl, historyPath, isPrivate ? cookie : ""),
		readPageTree(fetch, apiBaseUrl, pageTreePath, isPrivate ? cookie : "")
	]);
	const history: SnapshotHistoryResponse = loadedHistory ?? { items: [], total: 0, page: 1, pageSize: 100 };

	const requestedReturn = url.searchParams.get("from")?.split("#", 1)[0];
	const returnHref =
		requestedReturn?.startsWith("/") &&
		!requestedReturn.startsWith("//") &&
		!requestedReturn.includes("\\") &&
		!requestedReturn.startsWith("/snapshots/")
			? requestedReturn
			: "/";

	return {
		snapshot,
		history,
		historyAvailable: loadedHistory !== null,
		pageTree,
		pageTreeAvailable: pageTree !== null,
		returnHref,
		isPrivate
	};
};
