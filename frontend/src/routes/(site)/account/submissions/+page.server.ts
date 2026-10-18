import { loadSubmissions } from "$lib/server/submissions";
import type { PageServerLoad } from "./$types";

/** Own records require a session, including for accounts still completing registration. */
export const load: PageServerLoad = (event) => loadSubmissions(event, false);
