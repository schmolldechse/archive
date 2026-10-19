<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";
	import { TableRoot as TableMeta } from "$lib/components/ui/table";
	const { Story } = defineMeta({
		title: "Components/Table",
		component: TableMeta,
		parameters: {
			docs: {
				description: {
					component:
						"Composable native table elements with forwarded HTML attributes and Svelte snippets. Nest sections inside TableRoot, rows inside sections and cells inside rows. Wrap the table in a named TableViewport for keyboard-accessible horizontal scrolling. TableRow selected is visual only; provide native controls for selection semantics. Use the DataTable gallery for local data processing."
				}
			}
		}
	});
</script>

<script lang="ts">
	import {
		TableRoot,
		TableViewport,
		TableCaption,
		TableHeader,
		TableBody,
		TableFooter,
		TableRow,
		TableHead,
		TableCell
	} from "$lib/components/ui/table";
	import Badge from "$lib/components/ui/Badge.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	let value = $state("");
	const source = "https://example.invalid/supporting-text/a-long-source-value?label=long-supporting-text";
	async function copy(): Promise<void> {
		try {
			await navigator.clipboard.writeText(source);
			value = "Copied";
		} catch {
			value = "Copy unavailable";
		}
	}
</script>

<Story name="Structure" asChild>
	<TableViewport aria-label="Label comparison">
		<TableRoot>
			<TableCaption>Label comparison</TableCaption>
			<TableHeader><TableRow><TableHead>Label</TableHead><TableHead align="end">Value</TableHead></TableRow></TableHeader>
			<TableBody>
				<TableRow><TableHead scope="row">Label A</TableHead><TableCell align="end">12</TableCell></TableRow>
				<TableRow><TableHead scope="row">Label B</TableHead><TableCell align="end">24</TableCell></TableRow>
			</TableBody>
			<TableFooter
				><TableRow><TableHead scope="row">Total</TableHead><TableCell align="end">36</TableCell></TableRow></TableFooter
			>
		</TableRoot>
	</TableViewport>
</Story>
<Story name="Cell content" asChild>
	<TableViewport aria-label="Cell content">
		<TableRoot>
			<TableCaption>Cell content</TableCaption>
			<TableHeader
				><TableRow
					><TableHead>Label</TableHead><TableHead>Identifier</TableHead><TableHead>Status</TableHead><TableHead
						>Actions</TableHead
					></TableRow
				></TableHeader
			>
			<TableBody>
				<TableRow
					><TableCell>Label</TableCell><TableCell valueKind="record">row-a</TableCell><TableCell
						><Badge>Supporting text</Badge></TableCell
					><TableCell><Button>Action</Button></TableCell></TableRow
				>
			</TableBody>
		</TableRoot>
	</TableViewport>
</Story>
<Story name="Constrained width" asChild>
	<div class="constrained">
		<p>Tab to the table region to scroll with the keyboard. Long values remain complete.</p>
		<TableViewport aria-label="Long content">
			<TableRoot>
				<TableCaption>Long content</TableCaption>
				<TableHeader><TableRow><TableHead>Label</TableHead><TableHead>Value</TableHead></TableRow></TableHeader>
				<TableBody
					><TableRow
						><TableCell>Long label with wrapped supporting text</TableCell><TableCell valueKind="record"
							>{source}<Button variant="text" onclick={copy}>Copy value</Button></TableCell
						></TableRow
					></TableBody
				>
			</TableRoot>
		</TableViewport>
		{#if value}<p role="status">{value}</p>{/if}
	</div>
</Story>

<style>
	.constrained {
		max-inline-size: 22rem;
		min-inline-size: 0;
	}
	p {
		font-size: 0.875rem;
		color: var(--marginal-note);
	}
</style>
