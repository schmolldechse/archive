<script lang="ts" generics="T">
	import ArrowDown from "@lucide/svelte/icons/arrow-down";
	import ArrowUp from "@lucide/svelte/icons/arrow-up";
	import ArrowUpDown from "@lucide/svelte/icons/arrow-up-down";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import Button from "../Button.svelte";
	import Skeleton from "../Skeleton.svelte";
	import { InputRoot, InputLabel, InputControl } from "../input";
	import { SelectRoot, SelectLabel, SelectTrigger, SelectValue, SelectContent, SelectViewport, SelectItem } from "../select";
	import {
		PaginationRoot,
		PaginationList,
		PaginationPrevious,
		PaginationNext,
		PaginationPage,
		PaginationEllipsis
	} from "../pagination";
	import TableRoot from "./TableRoot.svelte";
	import TableViewport from "./TableViewport.svelte";
	import TableCaption from "./TableCaption.svelte";
	import TableHeader from "./TableHeader.svelte";
	import TableBody from "./TableBody.svelte";
	import TableRow from "./TableRow.svelte";
	import TableHead from "./TableHead.svelte";
	import TableCell from "./TableCell.svelte";
	import TableSelectionControl from "./TableSelectionControl.svelte";
	import {
		createDataTableState,
		prepareDataTableRows,
		deriveDataTableModel,
		getDataTablePageSizes,
		getNextDataTableSorting,
		changeDataTableSelection,
		equalDataTableStates
	} from "./data-table-model";
	import type { DataTableProps, DataTableState, DataTableChangeReason, DataTableStatus } from "./types";

	const generatedId = $props.id();
	let {
		data,
		columns,
		getRowId,
		label,
		state = $bindable(createDataTableState()),
		onStateChange,
		selection,
		searchable = true,
		pageSizeOptions = [10, 25, 50],
		loading = false,
		error = null,
		actions,
		emptyContent,
		errorContent,
		tableAttributes = {},
		class: className,
		...restProps
	}: DataTableProps<T> = $props();

	const rows = $derived(prepareDataTableRows(data, columns, getRowId, selection));
	const model = $derived(deriveDataTableModel(rows, columns, state, searchable));
	const pageSizes = $derived(getDataTablePageSizes(pageSizeOptions, state.pageSize));
	const status = $derived<DataTableStatus>(
		error ? "error" : loading ? "loading" : !data.length ? "empty" : !model.total ? "no-results" : "ready"
	);
	const inactive = $derived(loading || Boolean(error));
	const filterColumns = $derived(columns.filter((column) => column.filter));
	const hasSearch = $derived(searchable && columns.some((column) => column.getValue && column.searchable !== false));
	const hasFilters = $derived(
		Boolean((hasSearch && state.query.trim()) || Object.values(state.columnFilters).some((value) => value.trim()))
	);
	const columnCount = $derived(columns.length + (selection ? 1 : 0));
	const hasSort = $derived(columns.some((column) => column.sort));
	const descriptionIds = $derived(
		[
			...new Set(
				[
					...(tableAttributes["aria-describedby"]?.split(/\s+/) ?? []),
					`${generatedId}-status`,
					...(hasSort ? [`${generatedId}-sort-hint`] : [])
				].filter(Boolean)
			)
		].join(" ")
	);

	/** Commit one complete transition; derived normalization never writes props itself. */
	function commit(patch: Partial<DataTableState>, reason: DataTableChangeReason): void {
		if (inactive) return;
		const next = { ...model.state, ...patch };
		if (equalDataTableStates(state, next)) return;
		state = next;
		onStateChange?.(next, reason);
	}
	function setQuery(value: string): void {
		commit({ query: value, page: 1, selectedIds: [] }, "search");
	}
	function setFilter(columnId: string, value: string): void {
		const columnFilters = { ...model.state.columnFilters, [columnId]: value };
		if (!value) delete columnFilters[columnId];
		commit({ columnFilters, page: 1, selectedIds: [] }, "filter");
	}
	/** Records may use keys that also exist on Object.prototype. */
	function filterValue(columnId: string): string {
		return Object.hasOwn(state.columnFilters, columnId) ? state.columnFilters[columnId] : "";
	}
	function selectRows(ids: readonly string[], checked: boolean): void {
		commit({ selectedIds: changeDataTableSelection(model.state.selectedIds, ids, checked) }, "selection");
	}
	function nextSortLabel(columnId: string, columnLabel: string): string {
		const next = getNextDataTableSorting(state.sorting, columnId);
		return next ? `Sort ${columnLabel} ${next.direction}` : `Clear sorting for ${columnLabel}`;
	}
</script>

<div
	{...restProps}
	class={["data-table text-reading-ink max-w-full min-w-0 font-sans", className]}
	data-component="data-table"
	data-state={status}
	aria-busy={status === "loading" ? "true" : undefined}
>
	<div class="data-table__toolbar">
		{#if hasSearch}
			<div class="data-table__search">
				<InputRoot disabled={inactive}>
					<InputLabel>Search</InputLabel>
					<InputControl type="search" value={state.query} oninput={(event) => setQuery(event.currentTarget.value)} />
				</InputRoot>
			</div>
		{/if}
		{#each filterColumns as column (column.id)}
			<div class="data-table__filter">
				{#if column.filter?.type === "text"}
					<InputRoot disabled={inactive}>
						<InputLabel>{column.label}</InputLabel>
						<InputControl value={filterValue(column.id)} oninput={(event) => setFilter(column.id, event.currentTarget.value)} />
					</InputRoot>
				{:else if column.filter?.type === "select"}
					{@const filterOptions = column.filter.options}
					<SelectRoot
						type="single"
						value={filterValue(column.id)}
						disabled={inactive}
						onValueChange={(value) => setFilter(column.id, value ?? "")}
					>
						<SelectLabel>{column.label}</SelectLabel>
						<SelectTrigger
							><SelectValue
								>{#snippet children()}{filterOptions.find((option) => option.value === filterValue(column.id))?.label ??
										"All"}{/snippet}</SelectValue
							></SelectTrigger
						>
						<SelectContent
							><SelectViewport>
								<SelectItem value="" textValue="All">All</SelectItem>
								{#each filterOptions as option (option.value)}<SelectItem value={option.value} textValue={option.label}
										>{option.label}</SelectItem
									>{/each}
							</SelectViewport></SelectContent
						>
					</SelectRoot>
				{/if}
			</div>
		{/each}
		{#if hasSearch || filterColumns.length}
			<Button
				disabled={inactive || !hasFilters}
				onclick={() => commit({ query: "", columnFilters: {}, page: 1, selectedIds: [] }, "clear-filters")}
				>Clear filters</Button
			>
		{/if}
		{#if actions}
			<div class="data-table__actions">
				{@render actions({ state: model.state, selectedIds: model.state.selectedIds, selectedRows: model.selectedRows })}
			</div>
		{/if}
	</div>
	<div class="data-table__register">
		<p id={`${generatedId}-status`} role="status" aria-atomic="true">
			{#if status === "loading"}Loading data
			{:else if status === "error"}Data unavailable
			{:else}{model.start}–{model.end} of {model.total}{#if selection}
					{" · "}{model.state.selectedIds.length} selected · {model.selectedOnPage} on this page{/if}{/if}
		</p>
		{#if selection}
			<Button
				variant="text"
				disabled={inactive || !model.state.selectedIds.length}
				onclick={() => commit({ selectedIds: [] }, "clear-selection")}>Clear selection</Button
			>
		{/if}
	</div>
	{#if hasSort}<p id={`${generatedId}-sort-hint`} class="sr-only">
			Column header buttons cycle through ascending, descending, and original order.
		</p>{/if}
	<TableViewport aria-labelledby={`${generatedId}-caption`}>
		<TableRoot {...tableAttributes} aria-describedby={descriptionIds} aria-busy={status === "loading" ? "true" : undefined}>
			<TableCaption id={`${generatedId}-caption`}>{label}</TableCaption>
			<TableHeader>
				<TableRow>
					{#if selection}
						<TableHead class="data-table__selection-cell">
							<TableSelectionControl
								label="Select all rows on this page"
								checked={model.allPageSelected}
								indeterminate={model.somePageSelected}
								disabled={inactive || !model.selectablePageIds.length}
								onchange={(event) => selectRows(model.selectablePageIds, event.currentTarget.checked)}
							/>
						</TableHead>
					{/if}
					{#each columns as column (column.id)}
						{@const direction = state.sorting?.columnId === column.id ? state.sorting.direction : undefined}
						<TableHead align={column.align} aria-sort={direction} data-sort={direction ?? "none"}>
							{#if column.sort}
								<Button
									variant="text"
									class="data-table__sort"
									disabled={inactive}
									aria-label={nextSortLabel(column.id, column.label)}
									onclick={() => commit({ sorting: getNextDataTableSorting(state.sorting, column.id), page: 1 }, "sort")}
								>
									{column.label}
									{#if direction === "ascending"}<ArrowUp aria-hidden="true" />
									{:else if direction === "descending"}<ArrowDown aria-hidden="true" />
									{:else}<ArrowUpDown aria-hidden="true" />{/if}
								</Button>
							{:else}<span class="data-table__heading">{column.label}</span>{/if}
						</TableHead>
					{/each}
				</TableRow>
			</TableHeader>
			<TableBody>
				{#if status === "error"}
					<TableRow
						><TableCell colspan={columnCount}>
							<div class="data-table__feedback" role="group" aria-label="Data error">
								<CircleAlert aria-hidden="true" />
								<div>
									<p class="data-table__error-heading">Unable to display data</p>
									{#if errorContent}{@render errorContent(error!)}{:else}<p>{error}</p>{/if}
								</div>
							</div>
						</TableCell></TableRow
					>
				{:else if status === "loading"}
					<TableRow
						><TableCell colspan={columnCount}>
							<div class="data-table__loading">
								<span>Loading data</span><Skeleton variant="text" width="60%" /><Skeleton variant="text" width="85%" />
							</div>
						</TableCell></TableRow
					>
				{:else if status === "empty" || status === "no-results"}
					<TableRow
						><TableCell colspan={columnCount}>
							<div class="data-table__empty">
								{#if emptyContent}{@render emptyContent(status)}{:else}{status === "empty"
										? "No data available"
										: "No results found"}{/if}
							</div>
						</TableCell></TableRow
					>
				{:else}
					{#each model.visibleRows as entry (entry.id)}
						<TableRow
							selected={model.selectedSet.has(entry.id)}
							data-row-id={entry.id}
							data-disabled={selection && !entry.selectable ? "" : undefined}
						>
							{#if selection}
								<TableCell class="data-table__selection-cell">
									<TableSelectionControl
										label={`Select ${entry.label}`}
										checked={model.selectedSet.has(entry.id)}
										disabled={!entry.selectable}
										title={!entry.selectable ? "Not selectable" : undefined}
										onchange={(event) => selectRows([entry.id], event.currentTarget.checked)}
									/>
								</TableCell>
							{/if}
							{#each columns as column (column.id)}
								<TableCell align={column.align} valueKind={column.valueKind}>
									{#if column.cell}{@render column.cell(entry.row)}
									{:else if entry.values.get(column.id) != null}{entry.values.get(column.id)}
									{:else}<span aria-hidden="true">—</span><span class="sr-only">Not available</span>{/if}
								</TableCell>
							{/each}
						</TableRow>
					{/each}
				{/if}
			</TableBody>
		</TableRoot>
	</TableViewport>
	<div class="data-table__pagination">
		<div class="data-table__page-size">
			<SelectRoot
				type="single"
				value={String(state.pageSize)}
				disabled={inactive}
				onValueChange={(value) => {
					if (value) commit({ pageSize: Number(value), page: 1 }, "page-size");
				}}
			>
				<SelectLabel>Rows per page</SelectLabel>
				<SelectTrigger><SelectValue>{#snippet children()}{state.pageSize}{/snippet}</SelectValue></SelectTrigger>
				<SelectContent
					><SelectViewport>
						{#each pageSizes as size (size)}<SelectItem value={String(size)} textValue={String(size)}>{size}</SelectItem>{/each}
					</SelectViewport></SelectContent
				>
			</SelectRoot>
		</div>
		<PaginationRoot
			totalItems={model.total}
			pageSize={state.pageSize}
			page={model.state.page}
			disabled={inactive}
			alwaysShow
			label={`${label} pages`}
			siblingCount={0}
			onPageChange={(page) => commit({ page }, "page")}
		>
			{#snippet children({ items })}
				<PaginationList
					><PaginationPrevious />
					{#each items as item (item.key)}
						{#if item.type === "page"}<PaginationPage {item} />{:else}<PaginationEllipsis />{/if}
					{/each}
					<PaginationNext />
				</PaginationList>
			{/snippet}
		</PaginationRoot>
	</div>
</div>

<style>
	.data-table {
		display: grid;
		gap: 1rem;
	}
	.data-table__toolbar,
	.data-table__pagination,
	.data-table__register {
		display: flex;
		flex-wrap: wrap;
		align-items: end;
		gap: 1rem;
		min-inline-size: 0;
	}
	.data-table__toolbar:empty {
		display: none;
	}
	.data-table__search {
		flex: 1 1 14rem;
		min-inline-size: 0;
	}
	.data-table__filter {
		flex: 1 1 10rem;
		min-inline-size: 0;
	}
	.data-table__actions {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem;
		max-inline-size: 100%;
	}
	.data-table__register {
		align-items: center;
		justify-content: space-between;
		border-block: 1px solid var(--border-trace);
		padding-block: 0.5rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
	.data-table__register p {
		margin: 0;
		overflow-wrap: anywhere;
	}
	.data-table__pagination {
		justify-content: space-between;
		align-items: end;
	}
	.data-table__page-size {
		inline-size: 10rem;
		max-inline-size: 100%;
	}
	.data-table :global(.data-table__selection-cell) {
		inline-size: 3.75rem;
		padding-inline: 0.5rem;
	}
	.data-table :global(.data-table__sort) {
		justify-content: flex-start;
		min-inline-size: 6rem;
		padding-inline: 0.5rem;
		text-align: start;
	}
	.data-table__heading {
		display: inline-flex;
		align-items: center;
		min-block-size: 2.75rem;
	}
	.data-table__empty,
	.data-table__loading {
		padding-block: 2rem;
		color: var(--marginal-note);
	}
	.data-table__loading {
		display: grid;
		gap: 1rem;
	}
	.data-table__feedback {
		display: flex;
		align-items: start;
		gap: 0.75rem;
		padding-block: 1.5rem;
		border-inline-start: 3px solid var(--time-marker);
		padding-inline-start: 1rem;
	}
	.data-table__feedback > :global(svg) {
		flex: none;
		inline-size: 1.25rem;
		block-size: 1.25rem;
		margin-block-start: 0.25rem;
		color: var(--time-marker);
	}
	.data-table__feedback p {
		margin: 0;
	}
	.data-table__error-heading {
		font-weight: 600;
	}
	@media (max-width: 30rem) {
		.data-table__search,
		.data-table__filter {
			flex-basis: 100%;
		}
		.data-table__toolbar > :global(.button) {
			inline-size: 100%;
		}
		.data-table__pagination {
			align-items: start;
		}
	}
</style>
