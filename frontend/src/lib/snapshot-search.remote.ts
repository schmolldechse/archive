import { getRequestEvent, query } from "$app/server";
import { env } from "$env/dynamic/private";
import { env as publicEnv } from "$env/dynamic/public";
import type { ListSnapshotsData, SnapshotListResponse } from "$api";
import { error } from "@sveltejs/kit";
import type { SearchErrors } from "$lib/components/snapshot-search/search-state";

type SnapshotSearchQuery = NonNullable<ListSnapshotsData["query"]>;

export const searchSnapshotIndex = query<SnapshotSearchQuery, { results: SnapshotListResponse | null; errors: SearchErrors }>(
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
		if (!response.ok) {
			if (response.status === 400) {
				const problem = await response.json().catch(() => null);
				const fields: Record<string, keyof SearchErrors> = {
					sourceurl: "query",
					title: "title",
					tags: "tags",
					sourcetype: "sourceType",
					quality: "quality",
					capturedfrom: "from",
					captureduntil: "through",
					order: "order"
				};
				const errors: SearchErrors = {};
				for (const [field, messages] of Object.entries(problem?.errors ?? {})) {
					const key = fields[field.split(".").pop()!.toLowerCase()] ?? "form";
					if (Array.isArray(messages)) errors[key] = messages.filter((message) => typeof message === "string").join(" ");
				}
				if (!Object.keys(errors).length) errors.form = "The search parameters could not be validated.";
				return { results: null, errors };
			}
			error(502, "The archive index is temporarily unavailable. Please try again.");
		}

		return { results: (await response.json()) as SnapshotListResponse, errors: {} };
	}
);
