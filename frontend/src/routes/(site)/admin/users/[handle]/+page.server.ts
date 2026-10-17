import { error, redirect } from "@sveltejs/kit";
import type { PermissionCatalogResponse, UserAccessResponse } from "$api";
import { readAdmin, requireAdmin } from "$lib/server/admin";
import { pageHref, usersHref } from "$lib/admin/navigation";
import type { PageServerLoad } from "./$types";

/** Only this account's permissions are paginated here; user search remains on its parent page. */
export const load: PageServerLoad = async (event) => {
	const capabilities = requireAdmin(event, "users");
	const permissionQ = (event.url.searchParams.get("permissionQ") ?? "").trim();
	const pageValue = event.url.searchParams.get("permissionPage") ?? "1";
	if (permissionQ.length > 128 || !/^[1-9]\d*$/.test(pageValue) || Number(pageValue) > 10000)
		error(400, "Use a valid permission search and page number.");
	const [access, catalog] = await Promise.all([
		readAdmin<UserAccessResponse>(event, `/api/admin/users/${encodeURIComponent(event.params.handle)}/access`),
		readAdmin<PermissionCatalogResponse>(event, "/api/admin/permissions")
	]);
	if (!access.etag) error(503, "Access settings could not be validated. Please try again.");
	const permissionPageSize = 10;
	const matchingPermissions = catalog.value.items.filter((item) =>
		`${item.key} ${item.displayName} ${item.description}`.toLowerCase().includes(permissionQ.toLowerCase())
	);
	const lastPage = Math.max(1, Math.ceil(matchingPermissions.length / permissionPageSize));
	if (Number(pageValue) > lastPage) redirect(303, pageHref(event.url, "permissionPage", lastPage));
	return {
		access: access.value,
		etag: access.etag,
		catalog: catalog.value.items,
		capabilities,
		permissionQ,
		permissionPage: Number(pageValue),
		permissionPageSize,
		permissionTotal: matchingPermissions.length,
		permissionItems: matchingPermissions.slice(
			(Number(pageValue) - 1) * permissionPageSize,
			Number(pageValue) * permissionPageSize
		),
		usersHref: usersHref(event.url)
	};
};
