import { describe, expect, test } from "bun:test";
import type { SnapshotPageTreeResponse } from "$api";
import { buildSnapshotPageTree, pageRouteLabel } from "./snapshot-page-tree";

const validTree: SnapshotPageTreeResponse = {
	rootPath: "index.html",
	pages: [
		{ path: "index.html", parentPath: null, sourceUrl: "https://example.com/", title: "Example" },
		{ path: "pages/a.html", parentPath: "index.html", sourceUrl: "https://example.com/a", title: "A" },
		{ path: "pages/b.html", parentPath: "index.html", sourceUrl: "https://example.com/b?q=1", title: null },
		{ path: "pages/c.html", parentPath: "pages/a.html", sourceUrl: "https://example.com/c", title: "C" }
	]
};

describe("snapshot page tree normalization", () => {
	test("retains breadth-first order and derives hierarchy depth", () => {
		const model = buildSnapshotPageTree(validTree);

		expect(model?.nodes.map((node) => node.page.path)).toEqual(validTree.pages.map((page) => page.path));
		expect(model?.maxDepth).toBe(2);
		expect(model?.root.children.map((node) => node.page.path)).toEqual(["pages/a.html", "pages/b.html"]);
	});

	test("rejects duplicate, orphaned and cyclic nodes", () => {
		expect(buildSnapshotPageTree({ ...validTree, pages: [...validTree.pages, validTree.pages[1]] })).toBeNull();
		expect(
			buildSnapshotPageTree({
				...validTree,
				pages: validTree.pages.map((page) =>
					page.path === "pages/a.html" ? { ...page, parentPath: "pages/missing.html" } : page
				)
			})
		).toBeNull();
		expect(
			buildSnapshotPageTree({
				...validTree,
				pages: validTree.pages.map((page) => {
					if (page.path === "pages/a.html") return { ...page, parentPath: "pages/c.html" };
					return page;
				})
			})
		).toBeNull();
	});

	test("formats the captured route without its origin", () => {
		expect(pageRouteLabel("https://example.com/b?q=1#result")).toBe("/b?q=1#result");
	});
});
