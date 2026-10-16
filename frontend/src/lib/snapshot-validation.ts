export const snapshotLimits = {
	title: 512,
	description: 2_048,
	sourceUrl: 4_096,
	tag: 128,
	tags: 30
} as const;

export function validSourceUrl(value: string): boolean {
	try {
		const url = new URL(value.trim());
		url.hash = "";
		url.username = "";
		url.password = "";
		url.pathname = url.pathname.replace(/\/+$/, "") || "/";
		return (
			["http:", "https:"].includes(url.protocol) &&
			value.length <= snapshotLimits.sourceUrl &&
			url.href.length <= snapshotLimits.sourceUrl
		);
	} catch {
		return false;
	}
}
