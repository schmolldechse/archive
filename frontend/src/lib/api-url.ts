import { env } from "$env/dynamic/public";

/** Browser-reachable API origin, supplied at runtime by SvelteKit. */
export const apiOrigin = () => new URL(env.PUBLIC_API_BASE_URL || "http://localhost:5200").origin;
export const apiUrl = (path: string) => new URL(path, `${apiOrigin()}/`).toString();
