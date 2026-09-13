import type { PaginationItem, PaginationRange } from "./types";

function assertNonNegativeInteger(value: number, name: string): void {
	if (!Number.isInteger(value) || value < 0) {
		throw new Error(`Pagination ${name} must be a non-negative integer. Received ${String(value)}.`);
	}
}

export function getPaginationPageCount(totalItems: number, pageSize: number): number {
	assertNonNegativeInteger(totalItems, "totalItems");

	if (!Number.isInteger(pageSize) || pageSize < 1) {
		throw new Error(`Pagination pageSize must be a positive integer. Received ${String(pageSize)}.`);
	}

	return totalItems === 0 ? 0 : Math.ceil(totalItems / pageSize);
}

export function getEffectivePaginationPage(page: number, pageCount: number): number {
	if (pageCount === 0) return 0;

	const integerPage = Number.isFinite(page) ? Math.trunc(page) : 1;
	return Math.min(Math.max(integerPage, 1), pageCount);
}

export function createPaginationItems(
	pageCount: number,
	page: number,
	siblingCount: number,
	boundaryCount: number
): PaginationItem[] {
	assertNonNegativeInteger(pageCount, "pageCount");
	assertNonNegativeInteger(siblingCount, "siblingCount");
	assertNonNegativeInteger(boundaryCount, "boundaryCount");

	if (pageCount === 0) return [];

	const effectivePage = getEffectivePaginationPage(page, pageCount);
	const visiblePages = new Set<number>([effectivePage]);

	for (let value = 1; value <= Math.min(boundaryCount, pageCount); value += 1) {
		visiblePages.add(value);
	}

	for (let value = Math.max(pageCount - boundaryCount + 1, 1); value <= pageCount; value += 1) {
		visiblePages.add(value);
	}

	for (
		let value = Math.max(effectivePage - siblingCount, 1);
		value <= Math.min(effectivePage + siblingCount, pageCount);
		value += 1
	) {
		visiblePages.add(value);
	}

	const sortedPages = [...visiblePages].sort((left, right) => left - right);
	const items: PaginationItem[] = [];

	for (const value of sortedPages) {
		const previousPage = items.at(-1);
		const previousValue = previousPage?.type === "page" ? previousPage.value : undefined;

		if (previousValue !== undefined && value - previousValue === 2) {
			const missingPage = previousValue + 1;
			items.push({ type: "page", value: missingPage, key: `page-${missingPage}` });
		} else if (previousValue !== undefined && value - previousValue > 2) {
			items.push({ type: "ellipsis", key: value <= effectivePage ? "ellipsis-start" : "ellipsis-end" });
		}

		items.push({ type: "page", value, key: `page-${value}` });
	}

	return items;
}

export function getPaginationRange(totalItems: number, pageSize: number, page: number, pageCount: number): PaginationRange {
	if (totalItems === 0 || pageCount === 0) {
		return { start: 0, end: 0, total: 0 };
	}

	const effectivePage = getEffectivePaginationPage(page, pageCount);

	return {
		start: (effectivePage - 1) * pageSize + 1,
		end: Math.min(effectivePage * pageSize, totalItems),
		total: totalItems
	};
}
