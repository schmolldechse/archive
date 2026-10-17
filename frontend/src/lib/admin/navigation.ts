/** Preserve user-search context separately from permission filtering on the detail page. */
export function usersHref(url: URL): string {
	const query = new URLSearchParams();
	for (const key of ["q", "role", "page"]) {
		const value = url.searchParams.get(key);
		if (value) query.set(key, value);
	}
	return `/admin/users${query.size ? `?${query}` : ""}`;
}

/** Build native pagination links without resetting the other list's state. */
export function pageHref(url: URL, key: string, page: number): string {
	const query = new URLSearchParams(url.search);
	query.set(key, String(page));
	return `${url.pathname}?${query}`;
}
