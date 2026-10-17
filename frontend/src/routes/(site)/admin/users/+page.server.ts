import { error, redirect } from "@sveltejs/kit";
import type { UserListResponse } from "$api";
import { readAdmin, requireAdmin } from "$lib/server/admin";
import { pageHref } from "$lib/admin/navigation";
import type { PageServerLoad } from "./$types";

/** Search and pagination are URL-owned and rendered on the server. */
export const load: PageServerLoad = async (event) => {
	requireAdmin(event, "users");
	const q = (event.url.searchParams.get("q") ?? "").trim();
	const role = event.url.searchParams.get("role") ?? "";
	const pageValue = event.url.searchParams.get("page") ?? "1";
	if (q.length > 128 || !["", "USER", "ADMIN"].includes(role) || !/^[1-9]\d*$/.test(pageValue) || Number(pageValue) > 10000)
		error(400, "Use a valid search, role, and page number.");
	const query = new URLSearchParams({ page: pageValue, pageSize: "25" });
	if (q) query.set("q", q);
	if (role) query.set("role", role);
	const result = await readAdmin<UserListResponse>(event, `/api/admin/users?${query}`);
	const lastPage = Math.max(1, Math.ceil(Number(result.value.total) / Number(result.value.pageSize)));
	if (Number(pageValue) > lastPage) redirect(303, pageHref(event.url, "page", lastPage));
	return { users: result.value, filters: { q, role } };
};
