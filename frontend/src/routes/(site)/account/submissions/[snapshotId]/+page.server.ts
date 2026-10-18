import { loadSubmission } from "$lib/server/submissions";
import type { PageServerLoad } from "./$types";

/** Ownership is enforced again by the API, including history and screenshot reads. */
export const load: PageServerLoad = (event) => loadSubmission(event, false);
