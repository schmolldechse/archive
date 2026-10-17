<script lang="ts">
	import Button from "$lib/components/ui/Button.svelte";
	import Badge from "$lib/components/ui/Badge.svelte";
	import { DataTable, createDataTableState, type DataTableColumn, type DataTableChangeReason } from "$lib/components/ui/table";

	type ExampleMode =
		| "defaults"
		| "local"
		| "sorting"
		| "filters"
		| "pagination"
		| "selection"
		| "binding"
		| "changes"
		| "snippets"
		| "loading"
		| "error"
		| "empty"
		| "no-results"
		| "responsive";
	interface Row {
		id: string;
		label: string;
		group: string;
		amount: number | null;
		available: boolean;
		url: string;
	}
	let { mode = "local" }: { mode?: ExampleMode } = $props();
	const initialRows: Row[] = Array.from({ length: 26 }, (_, index) => ({
		id: `row-${String(index + 1).padStart(2, "0")}`,
		label: index < 2 ? "Label A" : `Label ${String.fromCharCode(65 + index)}`,
		group: index % 2 ? "Secondary" : "Primary",
		amount: index === 2 ? null : index % 7,
		available: index !== 3,
		url: `https://example.invalid/supporting-text/a-long-path-with-a-complete-copyable-value/${index + 1}?label=supporting-text`
	}));
	// Each mounted story owns its state; changing data does not silently reset it.
	let rows = $state(initialRows);
	let tableState = $state(createDataTableState({ pageSize: 5 }));
	let lastReason = $state<DataTableChangeReason | "none">("none");
	let callbackCount = $state(0);
	let excludeFirst = $state(false);
	let copyStatus = $state("");
	const visibleData = $derived(mode === "empty" ? [] : rows);
	const displayedState = $derived(mode === "no-results" ? { ...tableState, query: "No matching label" } : tableState);
	const columns = $derived<DataTableColumn<Row>[]>([
		{
			id: "label",
			label: "Label",
			getValue: (row) => row.label,
			sort: "text",
			filter: mode === "filters" ? { type: "text" } : undefined
		},
		{
			id: "group",
			label: "Group",
			getValue: (row) => row.group,
			sort: "text",
			filter: {
				type: "select",
				options: [
					{ value: "Primary", label: "Primary" },
					{ value: "Secondary", label: "Secondary" }
				]
			},
			cell: groupCell
		},
		{ id: "amount", label: "Value", getValue: (row) => row.amount, sort: "number", align: "end" },
		...(mode === "sorting"
			? [
					{
						id: "length",
						label: "Label length",
						getValue: (row: Row) => row.label.length,
						sort: (left: Row, right: Row) => left.label.length - right.label.length,
						align: "end" as const
					}
				]
			: []),
		...(mode === "responsive"
			? [
					{
						id: "source",
						label: "A long column label that wraps",
						getValue: (row: Row) => row.url,
						valueKind: "record" as const,
						cell: sourceCell
					}
				]
			: []),
		...(mode === "snippets" ? [{ id: "action", label: "Actions", cell: actionCell }] : [])
	]);

	function recordChange(_: unknown, reason: DataTableChangeReason): void {
		lastReason = reason;
		callbackCount += 1;
	}
	function reset(): void {
		tableState = createDataTableState({ pageSize: 5 });
	}
	async function copy(value: string): Promise<void> {
		try {
			await navigator.clipboard.writeText(value);
			copyStatus = "Copied";
		} catch {
			copyStatus = "Copy unavailable";
		}
	}
</script>

{#snippet groupCell(row: Row)}<Badge>{row.group}</Badge>{/snippet}
{#snippet sourceCell(row: Row)}
	<span class="source-value">{row.url}</span>
	<Button variant="text" onclick={() => copy(row.url)}>Copy value</Button>
{/snippet}
{#snippet actionCell(row: Row)}<Button
		onclick={() => {
			copyStatus = `Selected ${row.label}`;
		}}>Action</Button
	>{/snippet}
{#snippet additionalActions(context: { selectedIds: readonly string[] })}
	<Button
		disabled={!context.selectedIds.length}
		onclick={() => {
			copyStatus = `${context.selectedIds.length} selected`;
		}}>Action</Button
	>
{/snippet}
{#snippet emptyMessage(reason: "empty" | "no-results")}
	<strong>{reason === "empty" ? "No items" : "No matching items"}</strong>
	<p>Supporting text</p>
{/snippet}
{#snippet errorMessage(message: string)}<p>{message}</p>
	<Button
		onclick={() => {
			copyStatus = "Action requested";
		}}>Action</Button
	>{/snippet}

<div class:constrained={mode === "responsive"} class="example">
	<p class="example-note">
		{#if mode === "sorting"}Use header buttons to cycle sort direction. Equal values keep their input order; missing values stay
			last. Label length uses a custom comparison.
		{:else if mode === "filters"}Search and column filters combine. Changing a filter resets the page and clears selection.
		{:else if mode === "selection"}The header checkbox selects this page. Row D is unavailable; selections on other pages
			remain.
		{:else if mode === "binding"}Binding owns the complete state. An external reset does not invoke the callback.
		{:else if mode === "changes"}Requested IDs remain in bound state until a user transition. Effective selection excludes
			unavailable rows.
		{:else if mode === "responsive"}The complete value wraps and remains copyable. Only the table region may scroll
			horizontally.
		{:else}Tab moves between controls. Enter activates buttons; Space changes checkboxes. Use the theme toolbar to inspect both
			themes.{/if}
	</p>
	{#if mode === "binding"}<Button onclick={reset}>Reset state externally</Button>{/if}
	{#if mode === "changes"}
		<div class="example-actions">
			<Button
				onclick={() => {
					rows = rows.slice(1);
				}}>Remove first row</Button
			>
			<Button
				onclick={() => {
					rows = initialRows;
				}}>Restore rows</Button
			>
			<Button
				onclick={() => {
					rows = rows.slice(0, 2);
				}}>Keep two rows</Button
			>
			<Button
				onclick={() => {
					excludeFirst = !excludeFirst;
				}}>Toggle first row availability</Button
			>
			<Button onclick={reset}>Reset state externally</Button>
		</div>
	{/if}
	{#if mode === "defaults"}
		<DataTable data={visibleData.slice(0, 3)} {columns} getRowId={(row) => row.id} label="Labels" />
	{:else if mode === "no-results"}
		<DataTable data={visibleData} {columns} getRowId={(row) => row.id} label="Labels" state={displayedState} />
	{:else}
		<DataTable
			data={visibleData}
			{columns}
			getRowId={(row) => row.id}
			label="Labels"
			bind:state={tableState}
			onStateChange={recordChange}
			selection={{
				getLabel: (row) => `${row.label} (${row.id})`,
				canSelect: (row) => row.available && !(excludeFirst && row.id === "row-01")
			}}
			loading={mode === "loading"}
			error={mode === "error" ? "Supporting text" : null}
			actions={mode === "snippets" ? additionalActions : undefined}
			emptyContent={mode === "snippets" ? emptyMessage : undefined}
			errorContent={mode === "error" ? errorMessage : undefined}
		/>
	{/if}
	{#if mode === "binding" || mode === "changes"}
		<p class="example-note">Last callback: <code>{lastReason}</code> · Count: <code>{callbackCount}</code></p>
		<pre aria-label="Bound table state">{JSON.stringify(tableState, null, 2)}</pre>
	{/if}
	{#if mode === "snippets"}<Button
			onclick={() => {
				rows = rows.length ? [] : initialRows;
			}}>Toggle empty data</Button
		>{/if}
	{#if copyStatus}<p role="status">{copyStatus}</p>{/if}
</div>

<style>
	.example {
		display: grid;
		gap: 1rem;
		max-inline-size: 72rem;
		min-inline-size: 0;
	}
	.constrained {
		max-inline-size: 24rem;
	}
	.example-note {
		margin: 0;
		font-size: 0.875rem;
		color: var(--marginal-note);
		overflow-wrap: anywhere;
	}
	.example-actions {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem;
	}
	pre {
		max-inline-size: 100%;
		overflow-x: auto;
		font-family: var(--font-record);
		font-size: 0.8125rem;
	}
	.source-value {
		display: block;
		min-inline-size: 14rem;
		max-inline-size: 20rem;
		overflow-wrap: anywhere;
	}
</style>
