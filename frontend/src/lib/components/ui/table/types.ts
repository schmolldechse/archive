import type { Snippet } from "svelte";
import type {
	HTMLAttributes,
	HTMLTableAttributes,
	HTMLThAttributes,
	HTMLTdAttributes,
	SvelteHTMLElements
} from "svelte/elements";

/** Native table wrappers keep attributes on their corresponding HTML element. */
export interface TableRootProps extends Omit<HTMLTableAttributes, "children"> {
	children: Snippet;
}
/** Keyboard-scrollable regions require an accessible name. */
export type TableViewportProps = Omit<HTMLAttributes<HTMLDivElement>, "children"> & { children: Snippet } & (
		{ "aria-label": string } | { "aria-labelledby": string }
	);
export interface TableCaptionProps extends Omit<SvelteHTMLElements["caption"], "children"> {
	children: Snippet;
}
export interface TableSectionProps extends Omit<HTMLAttributes<HTMLTableSectionElement>, "children"> {
	children: Snippet;
}
export interface TableRowProps extends Omit<HTMLAttributes<HTMLTableRowElement>, "children"> {
	children: Snippet;
	/** Presentation only; native checkboxes provide selection semantics. */
	selected?: boolean;
}
export type TableAlign = "start" | "end";
export type TableValueKind = "text" | "record";
export interface TableHeadProps extends Omit<HTMLThAttributes, "children" | "align"> {
	children: Snippet;
	align?: TableAlign;
}
export interface TableCellProps extends Omit<HTMLTdAttributes, "children" | "align"> {
	children: Snippet;
	align?: TableAlign;
	valueKind?: TableValueKind;
}
/** Raw scalar values are independent of the formatted cell snippet. */
export type DataTableValue = string | number | null | undefined;
export type DataTableDirection = "ascending" | "descending";
export interface DataTableSorting {
	columnId: string;
	direction: DataTableDirection;
}
export interface DataTableState {
	sorting: DataTableSorting | null;
	query: string;
	columnFilters: Record<string, string>;
	/** One-based requested page. An empty result does not write page zero back. */
	page: number;
	pageSize: number;
	/** Requested stable IDs; unavailable IDs are excluded from the effective selection. */
	selectedIds: string[];
}
export type DataTableChangeReason =
	"sort" | "search" | "filter" | "page" | "page-size" | "selection" | "clear-filters" | "clear-selection";
export type DataTableFilter =
	| { type: "text" }
	| {
			type: "select";
			/** Values must be unique and nonempty; the empty filter means All. */
			options: readonly { value: string; label: string }[];
	  };
interface ColumnBase<T> {
	id: string;
	label: string;
	align?: TableAlign;
	valueKind?: TableValueKind;
	cell?: Snippet<[T]>;
}
/** Data columns opt into comparison and filters using raw, finite scalar values. */
export interface DataTableDataColumn<T> extends ColumnBase<T> {
	getValue: (row: T) => DataTableValue;
	sort?: "text" | "number" | ((left: T, right: T) => number);
	searchable?: boolean;
	filter?: DataTableFilter;
}
/** Display columns cannot accidentally participate in data processing. */
export interface DataTableDisplayColumn<T> extends ColumnBase<T> {
	cell: Snippet<[T]>;
	getValue?: never;
	sort?: never;
	searchable?: never;
	filter?: never;
}
export type DataTableColumn<T> = DataTableDataColumn<T> | DataTableDisplayColumn<T>;
export interface DataTableSelection<T> {
	getLabel: (row: T) => string;
	canSelect?: (row: T) => boolean;
}
export interface DataTableActionContext<T> {
	state: DataTableState;
	selectedIds: readonly string[];
	selectedRows: readonly T[];
}
export type DataTableEmptyReason = "empty" | "no-results";
export type DataTableStatus = "ready" | "loading" | "error" | DataTableEmptyReason;
type ManagedTableAttributes = "children" | "aria-busy" | "data-component";
/** A local table always receives the complete collection, never a server page. */
export interface DataTableProps<T> extends Omit<HTMLAttributes<HTMLDivElement>, "children" | "aria-busy"> {
	data: readonly T[];
	columns: readonly DataTableColumn<T>[];
	getRowId: (row: T) => string;
	label: string;
	/** Bind the entire state; callbacks notify rather than maintain a second source. */
	state?: DataTableState;
	onStateChange?: (state: DataTableState, reason: DataTableChangeReason) => void;
	selection?: DataTableSelection<T>;
	searchable?: boolean;
	pageSizeOptions?: readonly number[];
	loading?: boolean;
	error?: string | null;
	actions?: Snippet<[DataTableActionContext<T>]>;
	emptyContent?: Snippet<[DataTableEmptyReason]>;
	errorContent?: Snippet<[string]>;
	tableAttributes?: Omit<HTMLTableAttributes, ManagedTableAttributes>;
}
