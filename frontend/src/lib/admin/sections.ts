import type { adminAccess } from "./access";

/** Only implemented panel sections belong here; detail pages remain within their parent section. */
const sections = [
	{ key: "users", label: "Users", href: "/admin/users" },
	{ key: "snapshots", label: "Snapshots", href: "/admin/snapshots" }
] as const;

/** Navigation and the portal entry point use the same effective section permissions. */
export function availableAdminSections(access: ReturnType<typeof adminAccess>) {
	return sections.filter((section) => access.portal && access[section.key]);
}
