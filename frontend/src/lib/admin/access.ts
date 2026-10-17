import type { AuthenticationSessionResponse } from "$api";

/** Server and browser share operation prerequisites; the API remains authoritative. */
export function adminAccess(session: AuthenticationSessionResponse | null) {
	const has = (key: string) =>
		session?.isAuthenticated === true && session.user?.isRegistrationComplete === true && session.permissions.includes(key);
	const portal = has("admin.access");
	const users = portal && has("admin.users.access");
	return { portal, users, roles: users && has("users.assign_roles"), permissions: users && has("users.assign_permissions") };
}
