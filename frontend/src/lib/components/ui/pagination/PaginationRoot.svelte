<script lang="ts">
	import { createPaginationItems, getEffectivePaginationPage, getPaginationPageCount, getPaginationRange } from "./helpers";
	import { setPaginationContext } from "./pagination-context.svelte";
	import type { PaginationMode, PaginationPageState, PaginationRootProps } from "./types";

	let {
		totalItems,
		pageSize,
		page = $bindable(1),
		onPageChange,
		siblingCount = 1,
		boundaryCount = 1,
		getHref,
		disabled = false,
		alwaysShow = false,
		label = "Pagination",
		children,
		ref = $bindable(null),
		class: className,
		"aria-labelledby": ariaLabelledby,
		...restProps
	}: PaginationRootProps = $props();

	const pageCount = $derived(getPaginationPageCount(totalItems, pageSize));
	const effectivePage = $derived(getEffectivePaginationPage(page, pageCount));
	const items = $derived(createPaginationItems(pageCount, effectivePage, siblingCount, boundaryCount));
	const range = $derived(getPaginationRange(totalItems, pageSize, effectivePage, pageCount));
	const mode = $derived<PaginationMode>(getHref ? "link" : "button");
	const visible = $derived(alwaysShow || pageCount > 1);

	const isUnmodifiedActivation = (event: MouseEvent): boolean =>
		event.button === 0 && !event.altKey && !event.ctrlKey && !event.metaKey && !event.shiftKey;

	const isPageUnavailable = (targetPage: number): boolean =>
		disabled || pageCount === 0 || targetPage < 1 || targetPage > pageCount;

	const getPageState = (targetPage: number): PaginationPageState => {
		if (isPageUnavailable(targetPage)) return "disabled";
		return targetPage === effectivePage ? "current" : "available";
	};

	const select = (targetPage: number, event: MouseEvent): void => {
		if (isPageUnavailable(targetPage)) return;

		if (mode === "link") {
			if (isUnmodifiedActivation(event)) onPageChange?.(targetPage);
			return;
		}

		if (page === targetPage) return;

		page = targetPage;
		onPageChange?.(targetPage);
	};

	setPaginationContext({
		get disabled() {
			return disabled;
		},
		get mode() {
			return mode;
		},
		get page() {
			return effectivePage;
		},
		get pageCount() {
			return pageCount;
		},
		getHref(targetPage) {
			return getHref?.(targetPage) ?? "";
		},
		getPageState,
		isPageUnavailable,
		select
	});
</script>

{#if visible}
	<nav
		{...restProps}
		bind:this={ref}
		class={["pagination-root", className]}
		aria-label={ariaLabelledby ? undefined : label}
		aria-labelledby={ariaLabelledby}
		data-component="pagination"
		data-mode={mode}
		data-disabled={disabled ? "" : undefined}
	>
		{@render children({ items, range, pageCount })}
	</nav>
{/if}

<style>
	.pagination-root {
		max-width: 100%;
		color: var(--reading-ink);
		font-family: var(--font-interface);
	}
</style>
