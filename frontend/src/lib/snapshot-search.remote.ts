import { getRequestEvent, query } from "$app/server";
import { env } from "$env/dynamic/private";
import { env as publicEnv } from "$env/dynamic/public";
import type { ListSnapshotsData, SnapshotListResponse } from "$api";

type SnapshotSearchQuery = NonNullable<ListSnapshotsData["query"]>;

export const searchSnapshotIndex = query<SnapshotSearchQuery, SnapshotListResponse>(
	"unchecked",
	async (searchQuery) => {
		const apiBaseUrl = (env.INTERNAL_API_BASE_URL || publicEnv.PUBLIC_API_BASE_URL || "http://localhost:5200").replace(
			/\/$/,
			""
		);
		const parameters = new URLSearchParams();

		for (const [name, value] of Object.entries(searchQuery)) {
			if (value === undefined) continue;

			if (Array.isArray(value)) {
				for (const item of value) parameters.append(name, String(item));
			} else {
				parameters.set(name, String(value));
			}
		}

		const response = await getRequestEvent().fetch(`${apiBaseUrl}/api/snapshots?${parameters}`);
		if (!response.ok) throw new Error(`The archive API returned HTTP ${response.status}.`);

		return (await response.json()) as SnapshotListResponse;
	}
);
