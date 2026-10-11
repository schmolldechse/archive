/** Return destinations are local public pages, never an authentication endpoint. */
export function safeReturnUrl(value: string | null | undefined): string {
	if (!value || value.length > 2048) return "/";
	try {
		if (!value.startsWith("/") || value.startsWith("//") || /[\\\p{Cc}]/u.test(value)) return "/";
		const url = new URL(value, "https://archive.invalid");
		if (url.origin !== "https://archive.invalid" || /^\/(?:login|account|api)(?:\/|$)/i.test(url.pathname)) return "/";
		return value;
	} catch {
		return "/";
	}
}

export const loginHref = (returnUrl: string) => `/login?returnUrl=${encodeURIComponent(safeReturnUrl(returnUrl))}`;
export const completeHref = (returnUrl: string) =>
	`/account/complete?returnUrl=${encodeURIComponent(safeReturnUrl(returnUrl))}`;
