import { loadSubmission } from "$lib/server/submissions";
import type { PageServerLoad } from "./$types";

/** General snapshot records are available to administrators without enabling review actions. */
export const load: PageServerLoad = (event) => loadSubmission(event, true);
