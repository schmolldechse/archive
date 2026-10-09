import { env } from "$env/dynamic/public";
import type {
	AuthenticationProblemResponse,
	AuthenticationProviderResponse,
	AuthenticationSessionResponse,
	UpdateUserProfileRequest,
	UserResponse
} from "$lib/api";

export function apiUrl(path: string): string {
	return `${(env.PUBLIC_API_BASE_URL || "http://localhost:5200").replace(/\/$/, "")}${path}`;
}

export class AuthenticationApiError extends Error {
	constructor(
		public status: number,
		public problem: AuthenticationProblemResponse | null
	) {
		super(problem?.detail || "The profile service is temporarily unavailable. Please try again.");
	}
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
	const response = await fetch(apiUrl(path), {
		signal: AbortSignal.timeout(15_000),
		...init,
		credentials: "include"
	});
	if (!response.ok) {
		const problem = (await response.json().catch(() => null)) as AuthenticationProblemResponse | null;
		throw new AuthenticationApiError(response.status, problem);
	}
	return response.json() as Promise<T>;
}

export const getSession = () =>
	request<AuthenticationSessionResponse>("/api/auth/session", { signal: AbortSignal.timeout(3_000) });
export const getProviders = () =>
	request<AuthenticationProviderResponse[]>("/api/auth/providers", { signal: AbortSignal.timeout(3_000) });
export const updateProfile = (profile: UpdateUserProfileRequest) =>
	request<UserResponse>("/api/users/me", {
		method: "PUT",
		headers: { "Content-Type": "application/json" },
		body: JSON.stringify(profile)
	});
