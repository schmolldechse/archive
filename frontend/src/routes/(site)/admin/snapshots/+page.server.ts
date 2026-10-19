import { loadAdminSnapshots } from "$lib/server/submissions";
import type { PageServerLoad } from "./$types";

/** The general snapshot register independently requires snapshot administration access. */
export const load: PageServerLoad = (event) => loadAdminSnapshots(event);
