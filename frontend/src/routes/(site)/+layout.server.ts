import type { LayoutServerLoad } from "./$types";

export const load: LayoutServerLoad = ({ locals, depends, setHeaders }) => {
	depends("app:auth");
	setHeaders({ "cache-control": "private, no-store" });
	return { session: locals.session };
};
