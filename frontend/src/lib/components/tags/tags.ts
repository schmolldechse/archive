export function normalizeTags(value: string | readonly string[]): string[] {
	const tags = typeof value === "string" ? value.split(",") : value;
	return [...new Set(tags.map((tag) => tag.trim().replace(/^#+/, "").toLowerCase()).filter(Boolean))];
}
