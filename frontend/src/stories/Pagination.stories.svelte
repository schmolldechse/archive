<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import { PaginationRoot as PaginationRootMeta } from "$lib/components/ui/pagination";

	const { Story } = defineMeta({
		title: "Components/Pagination",
		component: PaginationRootMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A composable pagination family for known result sets, with native link navigation or locally controlled button behavior."
				}
			}
		}
	});
</script>

<script lang="ts">
	import {
		PaginationEllipsis,
		PaginationList,
		PaginationNext,
		PaginationPage,
		PaginationPrevious,
		PaginationRoot,
		type PaginationItem
	} from "$lib/components/ui/pagination";

	let buttonPage = $state(4);
	let lastButtonPage = $state<number | null>(null);
	let lastLinkPage = $state<number | null>(null);

	const getPageHref = (page: number): string => `#pagination-page-${page}`;
</script>

{#snippet PaginationControls(items: PaginationItem[])}
	<PaginationList>
		<PaginationPrevious />
		{#each items as item (item.key)}
			{#if item.type === "page"}
				<PaginationPage {item} />
			{:else}
				<PaginationEllipsis />
			{/if}
		{/each}
		<PaginationNext />
	</PaginationList>
{/snippet}

<Story name="Button Mode" asChild>
	<div class="story-stack">
		<p class="story-note">
			Activate a page with Enter or Space. The bound page and callback readout update from the same accepted activation.
		</p>
		<PaginationRoot
			totalItems={126}
			pageSize={15}
			bind:page={buttonPage}
			onPageChange={(page) => (lastButtonPage = page)}
			label="Button mode pagination"
		>
			{#snippet children({ items, range, pageCount })}
				{@render PaginationControls(items)}
				<p class="story-register" aria-live="polite">
					<span>Page</span>
					<code>{buttonPage} / {pageCount}</code>
					<span>Range</span>
					<code>{range.start}–{range.end} of {range.total}</code>
					<span>Last callback</span>
					<code>{lastButtonPage ?? "None"}</code>
				</p>
			{/snippet}
		</PaginationRoot>
	</div>
</Story>

<Story name="Link Mode" asChild>
	<div class="story-stack">
		<p class="story-note">
			Page controls are native links. Modified clicks retain browser behavior; an unmodified activation is reported without
			cancelling navigation.
		</p>
		<PaginationRoot
			totalItems={240}
			pageSize={20}
			page={6}
			getHref={getPageHref}
			onPageChange={(page) => (lastLinkPage = page)}
			label="Link mode pagination"
		>
			{#snippet children({ items, range, pageCount })}
				{@render PaginationControls(items)}
				<p class="story-register" aria-live="polite">
					<span>Page count</span>
					<code>{pageCount}</code>
					<span>Range</span>
					<code>{range.start}–{range.end} of {range.total}</code>
					<span>Last unmodified activation</span>
					<code>{lastLinkPage ?? "None"}</code>
				</p>
			{/snippet}
		</PaginationRoot>
		<div id="pagination-page-1" class="story-destination">Neutral link destination</div>
	</div>
</Story>

<Story name="Compact Ranges" asChild>
	<div class="story-grid">
		<section>
			<p class="story-label">Start</p>
			<PaginationRoot totalItems={500} pageSize={10} page={2} label="Start range pagination">
				{#snippet children({ items })}{@render PaginationControls(items)}{/snippet}
			</PaginationRoot>
		</section>
		<section>
			<p class="story-label">Middle</p>
			<PaginationRoot totalItems={500} pageSize={10} page={25} label="Middle range pagination">
				{#snippet children({ items })}{@render PaginationControls(items)}{/snippet}
			</PaginationRoot>
		</section>
		<section>
			<p class="story-label">End</p>
			<PaginationRoot totalItems={500} pageSize={10} page={49} label="End range pagination">
				{#snippet children({ items })}{@render PaginationControls(items)}{/snippet}
			</PaginationRoot>
		</section>
	</div>
</Story>

<Story name="Boundary and Sibling Counts" asChild>
	<div class="story-grid">
		<section>
			<p class="story-label">Two boundaries, no siblings</p>
			<PaginationRoot
				totalItems={300}
				pageSize={10}
				page={15}
				boundaryCount={2}
				siblingCount={0}
				label="Boundary count pagination"
			>
				{#snippet children({ items })}{@render PaginationControls(items)}{/snippet}
			</PaginationRoot>
		</section>
		<section>
			<p class="story-label">No boundaries, two siblings</p>
			<PaginationRoot
				totalItems={300}
				pageSize={10}
				page={15}
				boundaryCount={0}
				siblingCount={2}
				label="Sibling count pagination"
			>
				{#snippet children({ items })}{@render PaginationControls(items)}{/snippet}
			</PaginationRoot>
		</section>
	</div>
</Story>

<Story name="States" asChild>
	<div class="story-grid">
		<section>
			<p class="story-label">First page</p>
			<PaginationRoot totalItems={100} pageSize={10} page={1} label="First page pagination">
				{#snippet children({ items })}{@render PaginationControls(items)}{/snippet}
			</PaginationRoot>
		</section>
		<section>
			<p class="story-label">Last page</p>
			<PaginationRoot totalItems={100} pageSize={10} page={10} label="Last page pagination">
				{#snippet children({ items })}{@render PaginationControls(items)}{/snippet}
			</PaginationRoot>
		</section>
		<section>
			<p class="story-label">Disabled navigation</p>
			<PaginationRoot totalItems={100} pageSize={10} page={4} disabled label="Disabled pagination">
				{#snippet children({ items })}{@render PaginationControls(items)}{/snippet}
			</PaginationRoot>
		</section>
		<section>
			<p class="story-label">Single page, always shown</p>
			<PaginationRoot totalItems={7} pageSize={10} alwaysShow label="Single page pagination">
				{#snippet children({ items, range })}
					{@render PaginationControls(items)}
					<p class="story-register"><code>{range.start}–{range.end} of {range.total}</code></p>
				{/snippet}
			</PaginationRoot>
		</section>
		<section>
			<p class="story-label">Empty collection, always shown</p>
			<PaginationRoot totalItems={0} pageSize={10} alwaysShow label="Empty pagination">
				{#snippet children({ items, range })}
					{@render PaginationControls(items)}
					<p class="story-register"><code>{range.start}–{range.end} of {range.total}</code></p>
				{/snippet}
			</PaginationRoot>
		</section>
	</div>
</Story>

<Story name="Responsive Layout" asChild>
	<div class="story-constrained">
		<p class="story-note">The list wraps in document order inside this constrained width.</p>
		<PaginationRoot totalItems={10000} pageSize={10} page={500} label="Responsive pagination">
			{#snippet children({ items })}{@render PaginationControls(items)}{/snippet}
		</PaginationRoot>
	</div>
</Story>

<style>
	.story-stack {
		display: grid;
		max-width: 56rem;
		gap: 1.5rem;
	}

	.story-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(min(100%, 24rem), 1fr));
		gap: 2rem;
		max-width: 64rem;
	}

	.story-grid section {
		min-width: 0;
		border-block-end: 1px solid var(--border-trace);
		padding-block-end: 1.5rem;
	}

	.story-label,
	.story-note,
	.story-register,
	.story-destination {
		font-family: var(--font-interface);
	}

	.story-label {
		margin: 0 0 0.75rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		line-height: 1.33;
		text-transform: uppercase;
	}

	.story-note {
		margin: 0;
		border-inline-start: 2px solid var(--register-mark);
		padding-inline-start: 0.75rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.43;
	}

	.story-register {
		display: flex;
		flex-wrap: wrap;
		align-items: baseline;
		gap: 0.5rem 1rem;
		margin: 0.75rem 0 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.43;
	}

	.story-register code {
		color: var(--reading-ink);
		font-family: var(--font-record);
		font-variant-numeric: tabular-nums;
	}

	.story-constrained {
		display: grid;
		width: 20rem;
		max-width: 100%;
		gap: 1rem;
	}

	.story-destination {
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
</style>
