import { env } from "$env/dynamic/private";
import type { AuthenticationProvider, AuthenticationSession } from "$api";
import { apiOrigin } from "./api";

const backendOrigin = () => (env.INTERNAL_API_BASE_URL || apiOrigin()).replace(/\/$/, "");

export async function readSession(cookie: string | null): Promise<AuthenticationSession | null> {
	const anonymous: AuthenticationSession = { isAuthenticated: false, user: null };
	if (!cookie) return anonymous;

	try {
		const response = await fetch(`${backendOrigin()}/api/auth/session`, {
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
		const response = await fetch(`${backendOrigin()}/api/auth/providers`, {
			cache: "no-store",
			signal: AbortSignal.timeout(3000)
		});
		return response.ok ? ((await response.json()) as AuthenticationProvider[]) : [];
	} catch {
		return [];
	}
}
