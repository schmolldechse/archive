import { env } from "$env/dynamic/private";
import { apiOrigin } from "$lib/api-url";

/** Optional private Docker/service-network address. Never sent to the browser. */
export const backendOrigin = () => env.INTERNAL_API_BASE_URL?.replace(/\/+$/, "") || apiOrigin();
export const backendUrl = (path: string) => `${backendOrigin()}${path}`;
