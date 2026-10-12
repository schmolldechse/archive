import { redirect } from "@sveltejs/kit";
import { safeReturnUrl, completeHref } from "$lib/auth/return-url";
import { readProviders } from "$lib/auth/auth.server";
import type { ToastOptions } from "$lib/components/ui/toast";
import type { PageServerLoad } from "./$types";

export const load: PageServerLoad = async (event) => {
	event.depends("app:auth");
	const returnUrl = safeReturnUrl(event.url.searchParams.get("returnUrl"));
	const session = event.locals.session;
	const user = session?.user;
	if (user?.isRegistrationComplete) redirect(303, returnUrl);
	if (user) redirect(303, completeHref(returnUrl));
	const providers = session === null ? [] : await readProviders();
	let notice = loginError(event.url.searchParams.get("error"));
	if (session === null) {
		notice = {
			variant: "error",
			title: "Account unavailable",
			description: "Your session couldn’t be checked. Try again in a moment."
		};
	} else if (!providers.length) {
		notice = {
			variant: "error",
			title: "Sign-in unavailable",
			description: "We couldn’t load the sign-in providers. Please try again."
		};
	}
	return { returnUrl, providers, notice };
};

function loginError(code: string | null): ToastOptions | null {
	if (code === "cancelled")
		return {
			variant: "info",
			title: "Sign-in cancelled",
			description: "Choose a provider to try again whenever you’re ready."
		};
	if (code === "failed")
		return {
			variant: "error",
			title: "Sign-in could not be completed",
			description: "Try again, or come back a little later."
		};
	return null;
}
