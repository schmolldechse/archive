import { createContext } from "svelte";
import type { AuthenticationSession } from "$api";

interface AuthContext {
	/** null means the session service is unavailable, not signed out. */
	readonly session: AuthenticationSession | null;
}

// Each layout owns its context. Session data stays in SvelteKit's server load.
export const [getAuthContext, setAuthContext] = createContext<AuthContext>();
