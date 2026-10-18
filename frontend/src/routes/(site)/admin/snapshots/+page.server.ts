import { loadSubmissions } from "$lib/server/submissions";
import type { PageServerLoad } from "./$types";

/** The review page independently requires portal and snapshot-section permissions. */
export const load: PageServerLoad = (event) => loadSubmissions(event, true);
