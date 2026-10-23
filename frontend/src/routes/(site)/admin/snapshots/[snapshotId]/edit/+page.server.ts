import { loadSubmissionEdit } from "$lib/server/submissions";
import type { PageServerLoad } from "./$types";

/** Administrators can edit any snapshot after the route-level permission check. */
export const load: PageServerLoad = (event) => loadSubmissionEdit(event, true);
