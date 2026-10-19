/** Scroll within the home page without changing its URL, filters or browser history. */
export function scrollHomeSection(event: MouseEvent, section: "home" | "browse" | "about"): void {
	if (
		event.defaultPrevented ||
		event.button !== 0 ||
		event.altKey ||
		event.ctrlKey ||
		event.metaKey ||
		event.shiftKey ||
		window.location.pathname !== "/"
	)
		return;

	const target = document.getElementById(section);
	if (!target) return;

	event.preventDefault();
	target.scrollIntoView({
		behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "instant" : "smooth",
		block: "start"
	});
}
