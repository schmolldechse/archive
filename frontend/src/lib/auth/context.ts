import { getContext, setContext } from "svelte";
import type { AuthenticationSession } from "$api";

const key = Symbol("ArchiveAuthentication");
type AuthContext = { readonly session: AuthenticationSession | null };

export function setAuthContext(getSession: () => AuthenticationSession | null): void {
	setContext<AuthContext>(key, {
		get session() {
			return getSession();
		}
	});
}

export function getAuthContext(): AuthContext {
	const context = getContext<AuthContext>(key);
	if (!context) throw new Error("Account components require the site layout.");
	return context;
}
