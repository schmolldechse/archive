import type { AuthenticationProviderResponse, AuthenticationSessionResponse } from "$api";
import { backendUrl } from "$lib/server/api";

export async function readSession(cookie: string | null): Promise<AuthenticationSessionResponse | null> {
	const anonymous: AuthenticationSessionResponse = { isAuthenticated: false, user: null };
	if (!cookie) return anonymous;

	try {
		const response = await fetch(backendUrl("/api/auth/session"), {
			// Forward the session cookie to the configured API.
			headers: { cookie, accept: "application/json" },
			cache: "no-store",
			redirect: "error",
			signal: AbortSignal.timeout(3000)
		});
		if (response.status === 401) return anonymous;
		if (!response.ok) return null;
		return (await response.json()) as AuthenticationSessionResponse;
	} catch {
		return null;
	}
}

export async function readProviders(): Promise<AuthenticationProviderResponse[]> {
	try {
		const response = await fetch(backendUrl("/api/auth/providers"), {
			cache: "no-store",
			redirect: "error",
			signal: AbortSignal.timeout(3000)
		});
		return response.ok ? ((await response.json()) as AuthenticationProviderResponse[]) : [];
	} catch {
		return [];
	}
}
