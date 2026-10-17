<script lang="ts">
	import {
		PaginationRoot,
		PaginationList,
		PaginationPrevious,
		PaginationNext,
		PaginationPage,
		PaginationEllipsis
	} from "$lib/components/ui/pagination";
	let {
		total,
		pageSize,
		currentPage,
		href,
		label
	}: { total: number; pageSize: number; currentPage: number; href: (page: number) => string; label: string } = $props();
</script>

<PaginationRoot totalItems={total} {pageSize} page={currentPage} getHref={href} {label} siblingCount={0}>
	{#snippet children({ items })}
		<PaginationList
			><PaginationPrevious />{#each items as item (item.key)}{#if item.type === "page"}<PaginationPage
						{item}
					/>{:else}<PaginationEllipsis />{/if}{/each}<PaginationNext /></PaginationList
		>
	{/snippet}
</PaginationRoot>
