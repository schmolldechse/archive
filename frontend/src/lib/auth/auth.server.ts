import type { AuthenticationProvider, AuthenticationSession } from "$api";
import { backendUrl } from "$lib/server/api";

export async function readSession(cookie: string | null): Promise<AuthenticationSession | null> {
	const anonymous: AuthenticationSession = { isAuthenticated: false, user: null };
	if (!cookie) return anonymous;

	try {
		const response = await fetch(backendUrl("/api/auth/session"), {
			// Only the configured API receives this header. ASP.NET owns the cookie format.
			headers: { cookie, accept: "application/json" },
			cache: "no-store",
			redirect: "error",
			signal: AbortSignal.timeout(3000)
		});
		if (response.status === 401) return anonymous;
		if (!response.ok) return null;
		return (await response.json()) as AuthenticationSession;
	} catch {
		return null;
	}
}

export async function readProviders(): Promise<AuthenticationProvider[]> {
	try {
		const response = await fetch(backendUrl("/api/auth/providers"), {
			cache: "no-store",
			redirect: "error",
			signal: AbortSignal.timeout(3000)
		});
		return response.ok ? ((await response.json()) as AuthenticationProvider[]) : [];
	} catch {
		return [];
	}
}
