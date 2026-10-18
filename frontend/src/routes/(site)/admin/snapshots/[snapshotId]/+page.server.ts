import { loadSubmission } from "$lib/server/submissions";
import type { PageServerLoad } from "./$types";

/** Private metadata and lifecycle history are loaded behind the same section guard. */
export const load: PageServerLoad = (event) => loadSubmission(event, true);
