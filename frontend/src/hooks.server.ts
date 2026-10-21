import type { Handle } from "@sveltejs/kit";
import { readSession } from "$lib/auth/auth.server";

export const handle: Handle = async ({ event, resolve }) => {
	event.locals.session = await readSession(event.request.headers.get("cookie"));
	const response = await resolve(event);
	response.headers.set("X-Frame-Options", "DENY");
	response.headers.append("Content-Security-Policy", "frame-ancestors 'none'");
	return response;
};
