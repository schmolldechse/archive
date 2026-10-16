import { createContext } from "svelte";
import type { AuthenticationSessionResponse } from "$api";

interface AuthContext {
	/** null means the session service is unavailable, not signed out. */
	readonly session: AuthenticationSessionResponse | null;
}

// Each layout owns its context. Session data stays in SvelteKit's server load.
export const [getAuthContext, setAuthContext] = createContext<AuthContext>();
