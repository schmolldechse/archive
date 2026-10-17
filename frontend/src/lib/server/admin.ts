import { error, redirect, type RequestEvent } from "@sveltejs/kit";
import { adminAccess } from "$lib/admin/access";
import { loginHref } from "$lib/auth/return-url";
import { backendUrl } from "$lib/server/api";

/** Each protected load checks independently; layout loads may run in parallel or be reused. */
export function requireAdmin(
	event: Pick<RequestEvent, "locals" | "url"> & { depends: (...dependencies: string[]) => void },
	section: "portal" | "users" = "portal"
) {
	event.depends("app:auth");
	const session = event.locals.session;
	if (!session) error(503, "Your account could not be loaded. Please try again.");
	const access = adminAccess(session);
	// Visitors without portal access return to the public archive before any section data is loaded.
	if (!access.portal) redirect(303, "/");
	if (!access[section]) error(403, "You do not have access to this administration area.");
	return access;
}

/** Private read path forwards only the request cookie to the configured API. */
export async function readAdmin<T>(
	event: Pick<RequestEvent, "request" | "url">,
	path: string
): Promise<{ value: T; etag: string | null }> {
	let response: Response;
	try {
		response = await fetch(backendUrl(path), {
			headers: { cookie: event.request.headers.get("cookie") ?? "", accept: "application/json" },
			cache: "no-store",
			redirect: "error",
			signal: AbortSignal.timeout(10000)
		});
	} catch {
		error(503, "User administration is temporarily unavailable. Please try again.");
	}
	if (response.status === 401) redirect(303, loginHref(event.url.pathname + event.url.search));
	if (response.status === 403) error(403, "Your access to user administration has changed.");
	if (response.status === 404) error(404, "This user could not be found. Their handle may have changed.");
	if (!response.ok) error(503, "User administration is temporarily unavailable. Please try again.");
	return { value: (await response.json()) as T, etag: response.headers.get("etag") };
}
