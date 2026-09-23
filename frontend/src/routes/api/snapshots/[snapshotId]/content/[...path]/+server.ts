import { error } from "@sveltejs/kit";
import { env as privateEnv } from "$env/dynamic/private";
import { env as publicEnv } from "$env/dynamic/public";
import type { RequestHandler } from "./$types";

const snapshotIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const archivedContentPolicy = (origin: string) =>
	`default-src 'none'; img-src ${origin} data:; style-src ${origin} 'unsafe-inline' data:; ` +
	`font-src ${origin} data:; media-src ${origin} data:; frame-src ${origin}; ` +
	"script-src 'none'; connect-src 'none'; object-src 'none'; " +
	`form-action 'none'; base-uri ${origin}; frame-ancestors 'self'; sandbox`;

export const GET: RequestHandler = async ({ params, request, fetch }) => {
	if (
		!snapshotIdPattern.test(params.snapshotId) ||
		!params.path ||
		params.path.split("/").some((part) => part === ".." || part === ".")
	) {
		error(404, "Archived resource not found");
	}

	const apiBaseUrl = (privateEnv.INTERNAL_API_BASE_URL || publicEnv.PUBLIC_API_BASE_URL || "http://localhost:5200").replace(
		/\/$/,
		""
	);
	const resourcePath = params.path.split("/").map(encodeURIComponent).join("/");
	let response: Response;
	try {
		const range = request.headers.get("range");
		response = await fetch(`${apiBaseUrl}/api/snapshots/${params.snapshotId}/content/${resourcePath}`, {
			headers: range ? { range } : undefined
		});
	} catch {
		error(502, "Archived resource unavailable");
	}
	if (response.status === 404) error(404, "Archived resource not found");
	if (!response.ok && response.status !== 416) error(502, "Archived resource unavailable");

	const headers = new Headers();
	for (const name of [
		"content-type",
		"content-security-policy",
		"content-disposition",
		"x-content-type-options",
		"cache-control",
		"accept-ranges",
		"content-range"
	]) {
		const value = response.headers.get(name);
		if (value) headers.set(name, value);
	}
	// The iframe has an opaque origin; explicitly allow its archived assets from this route's origin.
	headers.set("content-security-policy", archivedContentPolicy(new URL(request.url).origin));
	headers.set("x-content-type-options", "nosniff");
	headers.set("referrer-policy", "no-referrer");
	// Fonts in an opaque-origin frame require CORS, while archived content is public.
	headers.set("access-control-allow-origin", "*");
	if (params.path === "index.html" && response.headers.get("content-type")?.includes("text/html")) {
		const stylesheet = `<link rel="stylesheet" href="${new URL("/archived-scrollbar.css", request.url).href}">`;
		const html = await response.text();
		const withScrollbar = /<\/head\s*>/i.test(html)
			? html.replace(/<\/head\s*>/i, `${stylesheet}$&`)
			: /<body\b/i.test(html)
				? html.replace(/<body\b/i, `${stylesheet}$&`)
				: `${stylesheet}${html}`;
		return new Response(withScrollbar, { status: response.status, headers });
	}
	return new Response(response.body, { status: response.status, headers });
};
