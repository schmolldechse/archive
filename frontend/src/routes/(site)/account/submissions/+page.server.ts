import { loadSubmissions } from "$lib/server/submissions";
import type { PageServerLoad } from "./$types";

/** Own records are available only after the account has completed registration. */
export const load: PageServerLoad = (event) => loadSubmissions(event);
