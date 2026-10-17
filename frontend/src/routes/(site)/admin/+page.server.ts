import { redirect } from "@sveltejs/kit";
import { requireAdmin } from "$lib/server/admin";
import { availableAdminSections } from "$lib/admin/sections";
import type { PageServerLoad } from "./$types";

export const load: PageServerLoad = (event) => {
	const [firstSection] = availableAdminSections(requireAdmin(event));
	if (firstSection) redirect(303, firstSection.href);
};
