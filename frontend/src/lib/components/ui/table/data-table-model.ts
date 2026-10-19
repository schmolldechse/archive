import type { DataTableColumn, DataTableSelection, DataTableSorting, DataTableState, DataTableValue } from "./types";

const textComparator = new Intl.Collator("en", { numeric: true, sensitivity: "base" });

/** Create an independent complete state for binding or an explicit dataset reset. */
export function createDataTableState(overrides: Partial<DataTableState> = {}): DataTableState {
	return {
		sorting: overrides.sorting ? { ...overrides.sorting } : null,
		query: overrides.query ?? "",
		columnFilters: { ...overrides.columnFilters },
		page: overrides.page ?? 1,
		pageSize: overrides.pageSize ?? 25,
		selectedIds: [...(overrides.selectedIds ?? [])]
	};
}

/** Cached accessors and identity are independent of filtering and pagination. */
export interface PreparedDataTableRow<T> {
	id: string;
	row: T;
	index: number;
	values: Map<string, DataTableValue>;
	selectable: boolean;
	label: string;
}

function requireLabel(value: string, name: string): void {
	if (typeof value !== "string" || !value.trim()) throw new Error(`DataTable ${name} must be a nonempty string.`);
}
function requirePositiveInteger(value: number, name: string): void {
	if (!Number.isSafeInteger(value) || value < 1) throw new Error(`DataTable ${name} must be a positive safe integer.`);
}

/** Reject ambiguous options before rendering controls with duplicate keys. */
export function getDataTablePageSizes(options: readonly number[], current: number): number[] {
	requirePositiveInteger(current, "pageSize");
	const seen = new Set<number>();
	for (const value of options) {
		requirePositiveInteger(value, "pageSizeOptions");
		if (seen.has(value)) throw new Error("DataTable pageSizeOptions must be unique.");
		seen.add(value);
	}
	seen.add(current);
	return [...seen].sort((a, b) => a - b);
}

/** Validate configuration once per data/column change and cache raw cell values. */
export function prepareDataTableRows<T>(
	data: readonly T[],
	columns: readonly DataTableColumn<T>[],
	getRowId: (row: T) => string,
	selection?: DataTableSelection<T>
): PreparedDataTableRow<T>[] {
	if (!columns.length) throw new Error("DataTable requires at least one column.");
	const columnIds = new Set<string>();
	for (const column of columns) {
		const columnId = column.id;
		requireLabel(column.id, "column id");
		requireLabel(column.label, "column label");
		if (columnIds.has(column.id)) throw new Error(`DataTable column id "${column.id}" must be unique.`);
		columnIds.add(column.id);
		if (!column.getValue && (column.sort || column.filter || column.searchable !== undefined)) {
			throw new Error(`DataTable display column "${columnId}" cannot process data.`);
		}
		if (column.filter?.type === "select") {
			const values = new Set<string>();
			for (const option of column.filter.options) {
				requireLabel(option.value, "filter option value");
				requireLabel(option.label, "filter option label");
				if (values.has(option.value)) throw new Error(`DataTable filter options for "${column.id}" must be unique.`);
				values.add(option.value);
			}
		}
	}
	const rowIds = new Set<string>();
	return data.map((row, index) => {
		const id = getRowId(row);
		requireLabel(id, "row id");
		if (rowIds.has(id)) throw new Error(`DataTable row id "${id}" must be unique.`);
		rowIds.add(id);
		const values = new Map<string, DataTableValue>();
		for (const column of columns) {
			if (!column.getValue) continue;
			const value = column.getValue(row);
			if (value != null && typeof value !== "string" && typeof value !== "number") {
				throw new Error(`DataTable column "${column.id}" must return a scalar value.`);
			}
			if (typeof value === "number" && !Number.isFinite(value)) {
				throw new Error(`DataTable column "${column.id}" must return finite numbers.`);
			}
			if (column.sort === "number" && value != null && typeof value !== "number") {
				throw new Error(`DataTable numeric column "${column.id}" must return numbers.`);
			}
			values.set(column.id, value);
		}
		const label = selection?.getLabel(row) ?? id;
		requireLabel(label, "selection label");
		return { id, row, index, values, selectable: Boolean(selection && (selection.canSelect?.(row) ?? true)), label };
	});
}

function normalizeText(value: DataTableValue): string {
	return String(value ?? "")
		.trim()
		.toLowerCase();
}

/** Derive the effective view without mutating requested state or input rows. */
export function deriveDataTableModel<T>(
	rows: readonly PreparedDataTableRow<T>[],
	columns: readonly DataTableColumn<T>[],
	state: DataTableState,
	searchable: boolean
) {
	requirePositiveInteger(state.pageSize, "pageSize");
	requirePositiveInteger(state.page, "page");
	const sortColumn = state.sorting ? columns.find((column) => column.id === state.sorting?.columnId) : undefined;
	if (state.sorting && (!sortColumn?.sort || !["ascending", "descending"].includes(state.sorting.direction))) {
		throw new Error("DataTable sorting must reference a sortable column and a valid direction.");
	}
	const activeFilters = Object.entries(state.columnFilters)
		.map(([id, value]) => {
			const column = columns.find((candidate) => candidate.id === id);
			if (!column?.filter || typeof value !== "string") throw new Error(`DataTable filter "${id}" is not supported.`);
			if (value && column.filter.type === "select" && !column.filter.options.some((option) => option.value === value)) {
				throw new Error(`DataTable filter value for "${id}" is not supported.`);
			}
			return { column, value };
		})
		.filter(({ value }) => Boolean(value.trim()));
	const searchColumns = columns.filter((column) => column.getValue && column.searchable !== false);
	const query = searchable && searchColumns.length ? normalizeText(state.query) : "";
	const filtered = rows.filter(
		(entry) =>
			(!query || searchColumns.some((column) => normalizeText(entry.values.get(column.id)).includes(query))) &&
			activeFilters.every(({ column, value }) =>
				column.filter?.type === "select"
					? entry.values.get(column.id) === value
					: normalizeText(entry.values.get(column.id)).includes(normalizeText(value))
			)
	);
	const sorted = sortColumn?.sort
		? [...filtered].sort((left, right) => {
				const a = left.values.get(sortColumn.id);
				const b = right.values.get(sortColumn.id);
				// Missing values remain last in either direction; only real comparisons reverse.
				if (a == null || b == null) return a == null && b == null ? left.index - right.index : a == null ? 1 : -1;
				const comparator = sortColumn.sort;
				let comparison: number;
				if (typeof comparator === "function") comparison = comparator(left.row, right.row);
				// Relational comparison avoids subtraction overflow for finite numeric values.
				else if (comparator === "number") comparison = a === b ? 0 : a < b ? -1 : 1;
				else comparison = textComparator.compare(String(a), String(b));
				if (!Number.isFinite(comparison)) throw new Error("DataTable comparators must return a finite number.");
				return (state.sorting?.direction === "descending" ? -comparison : comparison) || left.index - right.index;
			})
		: filtered;
	const total = sorted.length;
	const pageCount = Math.ceil(total / state.pageSize);
	const page = Math.min(state.page, Math.max(1, pageCount));
	const visibleRows = sorted.slice((page - 1) * state.pageSize, page * state.pageSize);
	const available = new Map(rows.filter((entry) => entry.selectable).map((entry) => [entry.id, entry]));
	const selectedIds = [...new Set(state.selectedIds)].filter((id) => available.has(id));
	const selectedSet = new Set(selectedIds);
	const selectablePage = visibleRows.filter((entry) => entry.selectable);
	const selectedOnPage = selectablePage.filter((entry) => selectedSet.has(entry.id)).length;
	return {
		state: { ...state, page, selectedIds },
		visibleRows,
		total,
		pageCount,
		selectedSet,
		selectedRows: selectedIds.map((id) => available.get(id)!.row),
		selectablePageIds: selectablePage.map((entry) => entry.id),
		selectedOnPage,
		allPageSelected: selectablePage.length > 0 && selectedOnPage === selectablePage.length,
		somePageSelected: selectedOnPage > 0 && selectedOnPage < selectablePage.length,
		start: total ? (page - 1) * state.pageSize + 1 : 0,
		end: Math.min(page * state.pageSize, total)
	};
}

/** Single-column cycle ends at the original input order. */
export function getNextDataTableSorting(current: DataTableSorting | null, columnId: string): DataTableSorting | null {
	if (current?.columnId !== columnId) return { columnId, direction: "ascending" };
	return current.direction === "ascending" ? { columnId, direction: "descending" } : null;
}

/** Page selection only edits supplied IDs, retaining selection on other pages. */
export function changeDataTableSelection(current: readonly string[], affected: readonly string[], checked: boolean): string[] {
	const result = new Set(current);
	for (const id of affected) {
		if (checked) result.add(id);
		else result.delete(id);
	}
	return [...result];
}

/** Avoid callbacks for identical transitions without depending on object key order. */
export function equalDataTableStates(a: DataTableState, b: DataTableState): boolean {
	const keys = Object.keys(a.columnFilters);
	return (
		a.page === b.page &&
		a.pageSize === b.pageSize &&
		a.query === b.query &&
		a.sorting?.columnId === b.sorting?.columnId &&
		a.sorting?.direction === b.sorting?.direction &&
		a.selectedIds.length === b.selectedIds.length &&
		a.selectedIds.every((id, index) => id === b.selectedIds[index]) &&
		keys.length === Object.keys(b.columnFilters).length &&
		keys.every((key) => a.columnFilters[key] === b.columnFilters[key])
	);
}
