import { error } from "@sveltejs/kit";
import { backendOrigin } from "$lib/server/api";
import type { SnapshotResponse } from "$api";
import type { PageServerLoad } from "./$types";

const snapshotIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export const load: PageServerLoad = async ({ params, url, fetch }) => {
	if (!snapshotIdPattern.test(params.snapshotId)) error(404, "Snapshot not found");

	const apiBaseUrl = backendOrigin();
	let response: Response;
	try {
		response = await fetch(`${apiBaseUrl}/api/snapshots/${params.snapshotId}`);
	} catch {
		error(502, "The archived page is temporarily unavailable.");
	}
	if (response.status === 404) error(404, "Snapshot not found");
	if (!response.ok) error(502, "The archived page is temporarily unavailable.");

	const snapshot = (await response.json()) as SnapshotResponse;
	const requestedReturn = url.searchParams.get("from");
	const returnHref =
		requestedReturn?.startsWith("/") &&
		!requestedReturn.startsWith("//") &&
		!requestedReturn.includes("\\") &&
		!requestedReturn.startsWith("/snapshots/")
			? requestedReturn
			: "/#snapshot-register";

	return { snapshot, returnHref };
};
