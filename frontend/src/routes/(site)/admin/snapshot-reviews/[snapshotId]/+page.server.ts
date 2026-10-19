import { loadSubmission } from "$lib/server/submissions";
import type { PageServerLoad } from "./$types";

/** Loads a private record and lifecycle history behind the review permission guard. */
export const load: PageServerLoad = (event) => loadSubmission(event, true);
