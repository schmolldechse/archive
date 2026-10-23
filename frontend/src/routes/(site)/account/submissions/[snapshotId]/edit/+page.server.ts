import { loadSubmissionEdit } from "$lib/server/submissions";
import type { PageServerLoad } from "./$types";

/** Ownership is enforced by the route guard and the underlying user snapshot API. */
export const load: PageServerLoad = (event) => loadSubmissionEdit(event);
