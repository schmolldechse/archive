import type { Snippet } from "svelte";
import type { HTMLAnchorAttributes, HTMLAttributes } from "svelte/elements";

export interface BreadcrumbProps extends Omit<HTMLAttributes<HTMLElement>, "children"> {
	children: Snippet;
}

export interface BreadcrumbListProps extends Omit<HTMLAttributes<HTMLOListElement>, "children"> {
	children: Snippet;
}

export interface BreadcrumbItemProps extends Omit<HTMLAttributes<HTMLLIElement>, "children"> {
	children: Snippet;
}

export interface BreadcrumbLinkProps extends Omit<HTMLAnchorAttributes, "children"> {
	children: Snippet;
}

export interface BreadcrumbPageProps extends Omit<HTMLAttributes<HTMLSpanElement>, "children" | "aria-current"> {
	children: Snippet;
}

export interface BreadcrumbSeparatorProps extends Omit<HTMLAttributes<HTMLLIElement>, "children"> {
	children?: Snippet;
}

export interface BreadcrumbEllipsisProps extends Omit<HTMLAttributes<HTMLSpanElement>, "children"> {
	label?: string;
}
