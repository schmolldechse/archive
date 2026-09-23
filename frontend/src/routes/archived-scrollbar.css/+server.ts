import scrollbarCss from "../../styles/scrollbar.css?raw";
import type { RequestHandler } from "./$types";

export const GET: RequestHandler = () =>
	new Response(scrollbarCss, {
		headers: {
			"content-type": "text/css; charset=utf-8",
			"cache-control": "public, max-age=3600",
			"x-content-type-options": "nosniff"
		}
	});
