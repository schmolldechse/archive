import type { Snippet } from "svelte";
import type { HTMLAttributes } from "svelte/elements";

export interface PaginationPageItem {
	type: "page";
	value: number;
	key: string;
}

export interface PaginationEllipsisItem {
	type: "ellipsis";
	key: string;
}

export type PaginationItem = PaginationPageItem | PaginationEllipsisItem;

export interface PaginationRange {
	start: number;
	end: number;
	total: number;
}

export interface PaginationRenderProps {
	items: PaginationItem[];
	range: PaginationRange;
	pageCount: number;
}

export type PaginationMode = "button" | "link";
export type PaginationPageState = "current" | "available" | "disabled";

type NativeNavProps = Omit<HTMLAttributes<HTMLElement>, "aria-disabled" | "aria-label" | "children">;
type NativeListProps = Omit<HTMLAttributes<HTMLUListElement>, "children">;
type NativeListItemProps = Omit<HTMLAttributes<HTMLLIElement>, "children">;

export interface PaginationRootProps extends NativeNavProps {
	totalItems: number;
	pageSize: number;
	page?: number;
	onPageChange?: (page: number) => void;
	siblingCount?: number;
	boundaryCount?: number;
	getHref?: (page: number) => string;
	disabled?: boolean;
	alwaysShow?: boolean;
	label?: string;
	children: Snippet<[PaginationRenderProps]>;
	ref?: HTMLElement | null;
}

export interface PaginationListProps extends NativeListProps {
	children: Snippet;
	ref?: HTMLUListElement | null;
}

export interface PaginationDirectionProps extends NativeListItemProps {
	label?: Snippet;
	"aria-label"?: string;
	ref?: HTMLLIElement | null;
}

export type PaginationPreviousProps = PaginationDirectionProps;
export type PaginationNextProps = PaginationDirectionProps;

export interface PaginationPageProps extends NativeListItemProps {
	item: PaginationPageItem;
	"aria-label"?: string;
	ref?: HTMLLIElement | null;
}

export interface PaginationEllipsisProps extends NativeListItemProps {
	label?: string | null;
	ref?: HTMLLIElement | null;
}
