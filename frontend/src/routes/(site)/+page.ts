import { redirect } from "@sveltejs/kit";
import { apiQuery, pageSize, readSearchState, searchHref } from "$lib/components/snapshot-search/search-state";
import { searchSnapshotIndex } from "$lib/snapshot-search.remote";
import type { PageLoad } from "./$types";

export const load: PageLoad = async ({ url }) => {
	const { state, errors } = readSearchState(url.searchParams);
	if (Object.keys(errors).length) return { state, pageSize, errors, results: null, apiAvailable: true, loadError: null };
	let results;
	try {
		const response = await searchSnapshotIndex(apiQuery(state));
		if (Object.keys(response.errors).length || !response.results)
			return { state, pageSize, errors: response.errors, results: null, apiAvailable: true, loadError: null };
		results = response.results;
	} catch (error) {
		return {
			state,
			pageSize,
			errors,
			results: null,
			apiAvailable: false,
			loadError: error instanceof Error ? error.message : "The archive index could not be loaded."
		};
	}
	const lastPage = Math.max(1, Math.ceil(Number(results.total) / pageSize));
	if (state.page > lastPage) redirect(307, searchHref({ ...state, page: lastPage }, "snapshot-register"));
	return { state, pageSize, errors, results, apiAvailable: true, loadError: null };
};
