import { requireAdmin } from "$lib/server/admin";
import { availableAdminSections } from "$lib/admin/sections";
import type { LayoutServerLoad } from "./$types";

/** The portal layout owns its navigation; pages also guard their own data loads. */
export const load: LayoutServerLoad = (event) => {
	const admin = requireAdmin(event);
	return { admin, sections: availableAdminSections(admin) };
};
