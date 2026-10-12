import type { Handle } from "@sveltejs/kit";
import { readSession } from "$lib/auth/auth.server";

export const handle: Handle = async ({ event, resolve }) => {
	event.locals.session = await readSession(event.request.headers.get("cookie"));
	return resolve(event);
};
