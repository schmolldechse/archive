import type { SnapshotPageResponse, SnapshotPageTreeResponse } from "$api";

export interface SnapshotPageNode {
	page: SnapshotPageResponse;
	children: SnapshotPageNode[];
	depth: number;
}

export interface SnapshotPageTreeModel {
	root: SnapshotPageNode;
	nodes: SnapshotPageNode[];
	maxDepth: number;
}

/** Builds a defensive client-side hierarchy while retaining the API's breadth-first order. */
export function buildSnapshotPageTree(tree: SnapshotPageTreeResponse): SnapshotPageTreeModel | null {
	if (!tree.pages.length) return null;

	const nodes = new Map<string, SnapshotPageNode>();
	for (const page of tree.pages) {
		if (!page.path || nodes.has(page.path)) return null;
		nodes.set(page.path, { page, children: [], depth: 0 });
	}

	const root = nodes.get(tree.rootPath);
	if (!root || root.page.parentPath !== null) return null;

	for (const node of nodes.values()) {
		if (node === root) continue;
		if (!node.page.parentPath || node.page.parentPath === node.page.path) return null;
		const parent = nodes.get(node.page.parentPath);
		if (!parent) return null;
		parent.children.push(node);
	}

	if ([...nodes.values()].some((node) => node !== root && node.page.parentPath === null)) return null;

	const visited = new Set<string>();
	const active = new Set<string>();
	let maxDepth = 0;
	const visit = (node: SnapshotPageNode, depth: number): boolean => {
		if (active.has(node.page.path) || visited.has(node.page.path)) return false;
		active.add(node.page.path);
		node.depth = depth;
		maxDepth = Math.max(maxDepth, depth);
		for (const child of node.children) {
			if (!visit(child, depth + 1)) return false;
		}
		active.delete(node.page.path);
		visited.add(node.page.path);
		return true;
	};

	if (!visit(root, 0) || visited.size !== nodes.size) return null;

	return {
		root,
		nodes: tree.pages.map((page) => nodes.get(page.path)!),
		maxDepth
	};
}

export function pageRouteLabel(sourceUrl: string): string {
	try {
		const url = new URL(sourceUrl);
		return `${url.pathname}${url.search}${url.hash}` || "/";
	} catch {
		return sourceUrl;
	}
}

export function pageDisplayTitle(page: SnapshotPageResponse): string {
	return page.title?.trim() || pageRouteLabel(page.sourceUrl);
}
