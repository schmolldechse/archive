import { error } from "@sveltejs/kit";
import { backendOrigin } from "$lib/server/api";
import type { SnapshotResponse, SnapshotSubmissionDetailResponse } from "$api";
import type { PageServerLoad } from "./$types";

const snapshotIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Reads private metadata only through existing owner or moderator authorization. */
async function readPrivateSnapshot(
	fetcher: typeof fetch,
	apiBaseUrl: string,
	cookie: string,
	scope: "admin" | "users",
	snapshotId: string
): Promise<SnapshotResponse | null> {
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
	return detail.snapshot.contentUrl ? detail.snapshot : null;
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
	if (response.status === 404) {
		const cookie = request.headers.get("cookie") ?? "";
		const authorizedSnapshot =
			(await readPrivateSnapshot(fetch, apiBaseUrl, cookie, "admin", params.snapshotId)) ??
			(await readPrivateSnapshot(fetch, apiBaseUrl, cookie, "users", params.snapshotId));
		if (!authorizedSnapshot) error(404, "Snapshot not found");
		snapshot = authorizedSnapshot;
		isPrivate = true;
		setHeaders({ "cache-control": "private, no-store" });
	} else {
		if (!response.ok) error(502, "The archived page is temporarily unavailable.");
		snapshot = (await response.json()) as SnapshotResponse;
	}

	const requestedReturn = url.searchParams.get("from")?.split("#", 1)[0];
	const returnHref =
		requestedReturn?.startsWith("/") &&
		!requestedReturn.startsWith("//") &&
		!requestedReturn.includes("\\") &&
		!requestedReturn.startsWith("/snapshots/")
			? requestedReturn
			: "/";

	return { snapshot, returnHref, isPrivate };
};
