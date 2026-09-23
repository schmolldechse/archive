import type { SourceType } from "$api";

export function uploadSourceLabel(sourceType: SourceType): string {
	switch (sourceType) {
		case "MHTML": return "Uploaded MHTML document";
		case "WEBARCHIVE": return "Uploaded Webarchive document";
		default: return "Uploaded HTML document";
	}
}

export function formatBytes(bytes: number): string {
	if (bytes < 1024) return `${bytes} B`;
	const units = ["KB", "MB", "GB", "TB"];
	const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)) - 1, units.length - 1);
	const value = bytes / 1024 ** (index + 1);
	return `${value.toFixed(value >= 10 ? 0 : 1)} ${units[index]}`;
}
