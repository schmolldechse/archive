import { getContext, setContext } from "svelte";
import type { AuthenticationProviderResponse, UserResponse } from "$lib/api";
import { apiUrl, getProviders, getSession } from "./api";

const key = Symbol("archive-authentication");

export class AuthenticationState {
	user = $state<UserResponse | null>(null);
	providers = $state<AuthenticationProviderResponse[]>([]);
	status = $state<"loading" | "ready" | "unavailable">("loading");
	private pending: Promise<void> | null = null;

	refresh(): Promise<void> {
		if (this.pending) return this.pending;
		if (this.status !== "ready") this.status = "loading";
		this.pending = this.load().finally(() => {
			this.pending = null;
		});
		return this.pending;
	}

	private async load(): Promise<void> {
		try {
			const [session, providers] = await Promise.all([getSession(), getProviders()]);
			this.user = session.user;
			this.providers = providers;
			this.status = "ready";
		} catch {
			this.user = null;
			this.providers = [];
			this.status = "unavailable";
		}
	}

	signOut(): void {
		const form = document.createElement("form");
		form.method = "post";
		form.action = apiUrl("/api/auth/logout");
		document.body.append(form);
		form.submit();
	}
}

export function setAuthenticationContext(): AuthenticationState {
	const state = new AuthenticationState();
	setContext(key, state);
	return state;
}
export function getAuthenticationContext(): AuthenticationState {
	const state = getContext<AuthenticationState>(key);
	if (!state) throw new Error("Authentication context requires the root layout.");
	return state;
}
