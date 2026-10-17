# Table and DataTable

Use the exports from `$lib/components/ui/table`. `TableRoot` and its children wrap native HTML elements and accept native attributes, event properties, classes and snippets. The `Components/Table` gallery documents their composition. `TableViewport` requires `aria-label` or `aria-labelledby` and provides keyboard scrolling without changing table semantics.

`DataTable<T>` handles a **complete local collection**. It filters before stable sorting and pagination. It does not fetch records, synchronize routes or interpret a server response. The `Components/DataTable` gallery documents its capabilities using independent, neutral examples.

## Minimal use

```svelte
<script lang="ts">
	import { DataTable, type DataTableColumn } from "$lib/components/ui/table";

	interface Row {
		id: string;
		label: string;
		value: number | null;
	}

	const rows: Row[] = [
		{ id: "row-a", label: "Label A", value: 12 },
		{ id: "row-b", label: "Label B", value: null }
	];
	const columns: DataTableColumn<Row>[] = [
		{ id: "label", label: "Label", getValue: (row) => row.label, sort: "text" },
		{ id: "value", label: "Value", getValue: (row) => row.value, sort: "number", align: "end" }
	];
</script>

<DataTable data={rows} {columns} getRowId={(row) => row.id} label="Labels" />
```

The default state uses page 1, 25 rows per page, no filters, no sorting and no selected IDs. The default page-size options are 10, 25 and 50. A valid externally requested size is included even if it is absent from those options.

## State and selection

For external access, initialize `let tableState = $state(createDataTableState({ pageSize: 10 }))` and pass `bind:state={tableState}`. This binds one complete state. `onStateChange(next, reason)` reports accepted user changes once, after assignment; it is not a separate controlled-state protocol. External changes and derived corrections do not trigger that callback.

Enable multi-selection with `selection={{ getLabel: (row) => row.label, canSelect: (row) => row.available }}`. Omit `canSelect` to allow every row. Labels should distinguish rows even when their visible data values are equal.

- The header checkbox only edits selectable IDs on the visible page. A mixed checkbox selects the remaining rows on that page.
- Selection persists across pages, page-size changes and sorting. Search or column-filter changes clear selection and return to page 1.
- Missing or unavailable IDs are excluded from effective selection. The next user transition commits this normalization. Reappearing IDs retain their requested selection until that transition; reset state explicitly for a different dataset.
- Row clicks do not select rows. Nested links and buttons keep their native behavior.

The `actions` snippet receives `{ state, selectedIds, selectedRows }` for effective selection. Feature actions, their permissions and their asynchronous behavior remain the consumer's responsibility.

## Columns and content

Data columns require an accessor returning a string, finite number or missing value. Text sort uses an explicit English collator; numeric sort compares raw numbers. Missing values stay last in either direction, and equal values retain input order. A custom row comparator can be supplied through `sort`; it must return a finite, deterministic result.

Use `filter: { type: "text" }` for substring matching, or `filter: { type: "select", options: [{ value: "primary", label: "Primary" }] }` for exact raw-string matching. Global search and column filters combine. Search is case-insensitive and trims comparison inputs; it does not remove accents or perform fuzzy matching. `searchable: false` excludes a column from global search. The table-level `searchable` prop hides and bypasses global search.

For custom cells, declare `{#snippet LabelCell(row: Row)}...{/snippet}` and pass it as the column's `cell`. Accessors continue to provide the processing value independently of rendered content. A display-only column has a `cell` snippet without an accessor, sort, filter or search setting. Technical records can use `valueKind: "record"`; ordinary labels retain the interface font.

`emptyContent(reason)` distinguishes `"empty"` from `"no-results"`. `errorContent(message)` customizes error details and optional consumer actions. `loading` and `error` display external feedback without initiating requests; error takes precedence over loading.

## Attributes and accessibility

Native `DataTable` attributes apply to its outer `div`. Use `tableAttributes` for the inner table, including a consumer description; its description IDs are merged with internal status and sort instructions. Classes are combined with base classes, and internal component/state markers remain authoritative.

The table keeps native semantics. Header buttons use Enter or Space, checkboxes use Space, and the named scroll region accepts arrow keys. Sort state is exposed on the active `th`; selection is exposed by native checked and indeterminate checkbox properties. A visual `TableRow selected` does not add selection semantics.

Keep long values complete. At narrow widths, surrounding controls wrap while the table scrolls inside its viewport. Both themes use the existing editorial tokens and global focus/reduced-motion rules. These are documentation stories; they contain no play functions or automated tests.
