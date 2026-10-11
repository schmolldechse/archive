import { env } from "$env/dynamic/public";

export const apiOrigin = () => new URL(env.PUBLIC_API_BASE_URL || "http://localhost:5200").origin;
export const apiUrl = (path: string) => `${apiOrigin()}${path}`;
