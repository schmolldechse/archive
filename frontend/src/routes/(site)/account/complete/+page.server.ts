import { fail, redirect } from "@sveltejs/kit";
import type { ValidationProblemDetails } from "$api";
import { loginHref, safeReturnUrl } from "$lib/auth/return-url";
import { backendUrl } from "$lib/server/api";
import type { Actions, PageServerLoad } from "./$types";

export const load: PageServerLoad = ({ locals, url, depends }) => {
	depends("app:auth");
	const returnUrl = safeReturnUrl(url.searchParams.get("returnUrl"));
	const session = locals.session;
	if (session && !session.user) redirect(303, loginHref(returnUrl));
	if (session?.user?.isRegistrationComplete) redirect(303, returnUrl);
	return { returnUrl, user: session?.user ?? null };
};

export const actions = {
	default: async ({ locals, request, url }) => {
		const returnUrl = safeReturnUrl(url.searchParams.get("returnUrl"));
		const data = await request.formData();
		const displayName = data.get("displayName");
		const handle = data.get("handle");
		const values = {
			displayName: typeof displayName === "string" ? displayName : "",
			handle: typeof handle === "string" ? handle : ""
		};
		const errors = { displayName: null as string | null, handle: null as string | null };
		const unavailable = "Your profile couldn’t be saved. Please try again.";
		if (locals.session === null) return fail(503, { values, errors, message: unavailable });
		if (!locals.session.user) redirect(303, loginHref(returnUrl));
		if (locals.session.user.isRegistrationComplete) redirect(303, returnUrl);

		let response: Response;
		try {
			response = await fetch(backendUrl("/api/users/profile"), {
				method: "PUT",
				headers: {
					cookie: request.headers.get("cookie") ?? "",
					"content-type": "application/json",
					accept: "application/json"
				},
				body: JSON.stringify(values),
				redirect: "error",
				signal: AbortSignal.timeout(10000)
			});
		} catch {
			return fail(503, { values, errors, message: unavailable });
		}

		if (response.status === 401) redirect(303, loginHref(returnUrl));
		if (response.ok) redirect(303, returnUrl);
		if (response.status === 400 || response.status === 409) {
			const problem: ValidationProblemDetails | null = await response.json().catch(() => null);
			errors.displayName = problem?.errors?.displayName?.join(" ") ?? null;
			errors.handle = problem?.errors?.handle?.join(" ") ?? null;
			return fail(response.status, {
				values,
				errors,
				message: errors.displayName || errors.handle ? null : unavailable
			});
		}
		return fail(503, { values, errors, message: unavailable });
	}
} satisfies Actions;
