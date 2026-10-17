import { requireAdmin } from "$lib/server/admin";
import type { LayoutServerLoad } from "./$types";

/** The portal layout owns its navigation; pages also guard their own data loads. */
export const load: LayoutServerLoad = (event) => ({ admin: requireAdmin(event) });
