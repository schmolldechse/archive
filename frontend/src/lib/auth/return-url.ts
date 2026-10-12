/** Return destinations are local public pages, never an authentication endpoint. */
export function safeReturnUrl(value: string | null | undefined): string {
	if (!value || value.length > 2048) return "/";
	try {
		if (!value.startsWith("/") || value.startsWith("//") || /[\\\p{Cc}]/u.test(value)) return "/";
		const url = new URL(value, "https://archive.invalid");
		const path = decodeURIComponent(url.pathname);
		if (
			url.origin !== "https://archive.invalid" ||
			/[\\%\p{Cc}]/u.test(path) ||
			/%2f/i.test(url.pathname) ||
			/^\/(?:login|account|api)(?:\/|$)/i.test(path)
		)
			return "/";
		return url.pathname + url.search + url.hash;
	} catch {
		return "/";
	}
}

export function withReturnUrl(path: string, returnUrl: string): string {
	const destination = safeReturnUrl(returnUrl);
	if (destination === "/") return path;
	return `${path}${path.includes("?") ? "&" : "?"}returnUrl=${encodeURIComponent(destination)}`;
}

export const loginHref = (returnUrl: string) => withReturnUrl("/login", returnUrl);
export const completeHref = (returnUrl: string) => withReturnUrl("/account/complete", returnUrl);
