import { redirect } from "@sveltejs/kit";
import { requireAdmin } from "$lib/server/admin";
import type { PageServerLoad } from "./$types";

export const load: PageServerLoad = (event) => {
	if (requireAdmin(event).users) redirect(303, "/admin/users");
};
