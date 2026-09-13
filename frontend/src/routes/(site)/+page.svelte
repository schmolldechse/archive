<script lang="ts">
	import ChevronRight from "@lucide/svelte/icons/chevron-right";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import { goto } from "$app/navigation";
	import type { SnapshotResponse } from "$api";
	import ArchiveRecord from "$lib/components/archive/ArchiveRecord.svelte";
	import DateRangeFilter, { type CaptureRange } from "$lib/components/archive/DateRangeFilter.svelte";
	import SnapshotPreview from "$lib/components/archive/SnapshotPreview.svelte";
	import LocalTimestamp from "$lib/components/site/LocalTimestamp.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import { InputControl, InputLabel, InputRoot } from "$lib/components/ui/input";
	import {
		PaginationEllipsis,
		PaginationList,
		PaginationNext,
		PaginationPage,
		PaginationPrevious,
		PaginationRoot
	} from "$lib/components/ui/pagination";
	import { onDestroy } from "svelte";
	import type { PageProps } from "./$types";

	let { data }: PageProps = $props();
	let previewOpen = $state(false);
	let selectedSnapshot = $state<SnapshotResponse | null>(null);
	let previewTrigger = $state<HTMLButtonElement | null>(null);
	let notice = $state<string | null>(null);
	let searchTimer: ReturnType<typeof setTimeout> | undefined;
	let noticeTimer: ReturnType<typeof setTimeout> | undefined;

	const snapshots = $derived(data.results?.items ?? []);
	const total = $derived(Number(data.results?.total ?? 0));
	const recentCaptures = $derived(snapshots.slice(0, 4));
	const registerNumber = $derived(String(total).padStart(3, "0"));
	const hasActiveRange = $derived(Boolean(data.from || data.to));

	function buildTarget(query: string, range: CaptureRange, page = 1): string {
		const parameters = new URLSearchParams();
		const trimmedQuery = query.trim();

		if (trimmedQuery) parameters.set("text", trimmedQuery);
		if (range.from) parameters.set("capturedFrom", range.from);
		if (range.to) parameters.set("capturedUntil", range.to);
		if (page > 1) parameters.set("page", String(page));

		const serialized = parameters.toString();
		return `/${serialized ? `?${serialized}` : ""}#browse`;
	}

	async function navigateIndex(query: string, range: CaptureRange, scrollToResults: boolean): Promise<void> {
		previewOpen = false;
		await goto(buildTarget(query, range), {
			keepFocus: !scrollToResults,
			noScroll: true,
			replaceState: !scrollToResults
		});

		if (scrollToResults) {
			const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
			document
				.querySelector("#catalogue-title")
				?.scrollIntoView({ behavior: reducedMotion ? "auto" : "smooth", block: "start" });
		}
	}

	function submitSearch(event: SubmitEvent): void {
		event.preventDefault();
		if (searchTimer) window.clearTimeout(searchTimer);

		const formData = new FormData(event.currentTarget as HTMLFormElement);
		void navigateIndex(String(formData.get("text") ?? ""), { from: data.from, to: data.to }, true);
	}

	function scheduleSearch(event: Event): void {
		if (searchTimer) window.clearTimeout(searchTimer);
		const query = (event.currentTarget as HTMLInputElement).value;

		searchTimer = window.setTimeout(() => {
			void navigateIndex(query, { from: data.from, to: data.to }, false);
		}, 500);
	}

	function applyRange(range: CaptureRange): void {
		void navigateIndex(data.query, range, true);
	}

	function clearRange(): void {
		void navigateIndex(data.query, {}, true);
	}

	function inspectSnapshot(snapshot: SnapshotResponse, trigger: HTMLButtonElement): void {
		selectedSnapshot = snapshot;
		previewTrigger = trigger;
		previewOpen = true;
	}

	function showNotice(message: string): void {
		if (noticeTimer) window.clearTimeout(noticeTimer);
		notice = message;
		noticeTimer = window.setTimeout(() => (notice = null), 3200);
	}

	async function copySource(snapshot: SnapshotResponse): Promise<void> {
		const source = snapshot.sourceUrl ?? snapshot.originalLink;
		if (!source) return;

		try {
			await navigator.clipboard.writeText(source);
			showNotice("Source URL copied to the clipboard.");
		} catch {
			showNotice("The source URL could not be copied. Select the visible URL instead.");
		}
	}

	onDestroy(() => {
		if (searchTimer) window.clearTimeout(searchTimer);
		if (noticeTimer) window.clearTimeout(noticeTimer);
	});
</script>

<svelte:head>
	<title>{data.query ? `Search: ${data.query} — ARCHIV` : "ARCHIV — Public web record"}</title>
	<meta
		name="description"
		content="Search immutable, time-stamped captures of the public web with their source, context, and provenance intact."
	/>
</svelte:head>

{#if notice}
	<div class="page-notice" role="status" aria-live="polite">{notice}</div>
{/if}

<main class="main-shell" id="main-content">
	<section class="opening" id="home" aria-labelledby="opening-title">
		<div class="opening__copy">
			<p class="eyebrow">An open register of preserved pages</p>
			<h1 id="opening-title">The web has a past. Keep it legible.</h1>
			<p class="lede">
				Search immutable, time-stamped captures of the public web — preserved with their source, context, and visible history
				intact.
			</p>
		</div>

		<aside class="opening__index" aria-label="A sequence of recent capture times">
			<div class="index-heading">
				<span>Register {registerNumber}</span>
				<span>Local offset</span>
			</div>
			<p class="index-statement">A page is not one thing. It is every state we can prove.</p>
			{#if recentCaptures.length}
				<ol class="index-sequence">
					{#each recentCaptures as snapshot, index (snapshot.id)}
						<li class:index-sequence__latest={index === 0}>
							<span class="sequence-point" aria-hidden="true"></span>
							<LocalTimestamp value={snapshot.createdAt} />
						</li>
					{/each}
				</ol>
			{:else}
				<p class="index-empty">The register has no visible captures yet.</p>
			{/if}
		</aside>
	</section>

	<section class="search-register" id="browse" aria-labelledby="search-title">
		<div class="search-register__heading">
			<div>
				<p class="section-label">Search the record</p>
				<h2 id="search-title">Find a page in time.</h2>
			</div>
			<p>Search across exact URLs, titles, descriptions, and tags. Newest records appear first.</p>
		</div>

		<form class="search-form" role="search" onsubmit={submitSearch}>
			<InputRoot id="homepage-search" class="search-form__field">
				<InputLabel>URL, title, description, or tag</InputLabel>
				<InputControl
					type="search"
					name="text"
					value={data.query}
					autocomplete="off"
					placeholder="Paste a URL or enter a phrase"
					data-value-kind="record"
					oninput={scheduleSearch}
				/>
			</InputRoot>
			<Button variant="primary" size="large" class="search-form__submit" type="submit">
				Search
				<ChevronRight aria-hidden="true" />
			</Button>
		</form>

		<div class="search-examples" aria-label="Example searches">
			<span>Try an index term:</span>
			{#each ["preservation", "open web", "public records"] as example}
				<Button
					variant="text"
					class="search-examples__action"
					onclick={() => void navigateIndex(example, { from: data.from, to: data.to }, true)}
				>
					{example}
				</Button>
			{/each}
		</div>
	</section>

	<section class="catalogue" aria-labelledby="catalogue-title">
		<div class="catalogue__heading">
			<div>
				<p class="section-label">Public index · newest first</p>
				<h2 id="catalogue-title">{data.query || hasActiveRange ? "Matching records" : "Recent records"}</h2>
			</div>
			<p class="result-status" aria-live="polite">
				{#if data.results}
					Showing {snapshots.length}{total > snapshots.length ? ` of ${total}` : ""}
					{total === 1 ? " record" : " records"}
				{:else}
					Index unavailable
				{/if}
			</p>
		</div>

		{#if data.filterError}
			<div class="status-message status-message--error" role="alert">
				<CircleAlert aria-hidden="true" />
				<div>
					<h3>Invalid capture range</h3>
					<p>{data.filterError}</p>
				</div>
			</div>
		{/if}

		<div class="catalogue__grid">
			<aside aria-label="Filter snapshots by capture range">
				<DateRangeFilter from={data.from} to={data.to} onApply={applyRange} onClear={clearRange} />
			</aside>

			<div>
				{#if !data.apiAvailable}
					<div class="status-message status-message--warning" role="status">
						<CircleAlert aria-hidden="true" />
						<div>
							<h3>The archive index is unavailable</h3>
							<p>{data.loadError ?? "The backend could not be reached. Try again later."}</p>
						</div>
					</div>
				{:else if snapshots.length}
					<ol class="record-list" aria-label="Archive records">
						{#each snapshots as snapshot (snapshot.id)}
							<li>
								<ArchiveRecord
									{snapshot}
									selected={previewOpen && selectedSnapshot?.id === snapshot.id}
									onInspect={inspectSnapshot}
									onCopy={(record) => void copySource(record)}
								/>
							</li>
						{/each}
					</ol>
					<PaginationRoot
						totalItems={total}
						pageSize={data.pageSize}
						page={data.page}
						getHref={(page) => buildTarget(data.query, { from: data.from, to: data.to }, page)}
						class="catalogue__pagination"
						label="Archive search result pages"
					>
						{#snippet children({ items })}
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
					</PaginationRoot>
				{:else}
					<div class="empty-register">
						<div class="empty-register__mark" aria-hidden="true"><span></span></div>
						<h3>No preserved pages found</h3>
						<p>Try the complete URL, a broader phrase, or a wider capture range.</p>
						<Button variant="text" onclick={() => void navigateIndex("", {}, false)}>Clear the search and dates</Button>
					</div>
				{/if}
			</div>
		</div>
	</section>

	<section class="principles" id="about" aria-labelledby="principles-title">
		<div>
			<p class="section-label">What the record means</p>
			<h2 id="principles-title">Preserved, not rewritten.</h2>
			<p class="principles__intro">Every published snapshot is public, immutable, and identified by its capture time.</p>
		</div>

		<dl class="definition-list">
			<div>
				<dt>Source</dt>
				<dd>The complete origin URL remains visible and copyable beside every record.</dd>
			</div>
			<div>
				<dt>Capture</dt>
				<dd>Rendered pages, loaded resources, metadata, and a viewport screenshot are kept together.</dd>
			</div>
			<div>
				<dt>Provenance</dt>
				<dd>Quality, resource count, size, and a stable public identifier describe what was preserved.</dd>
			</div>
		</dl>
	</section>
</main>

<SnapshotPreview bind:open={previewOpen} snapshot={selectedSnapshot} returnFocus={() => previewTrigger} />

<style>
	.main-shell {
		width: min(100%, 86rem);
		margin-inline: auto;
		padding-inline: clamp(1rem, 4vw, 4rem);
	}

	.page-notice {
		position: fixed;
		z-index: 50;
		top: 1rem;
		left: 50%;
		max-width: calc(100vw - 2rem);
		transform: translateX(-50%);
		border: 1px solid var(--preservation-green);
		border-radius: var(--radius-control);
		background: var(--reading-room);
		box-shadow: var(--shadow-overlay);
		padding: 0.75rem 1rem;
		font-size: 0.875rem;
		font-weight: 600;
	}

	.opening {
		display: grid;
		min-height: 34rem;
		grid-template-columns: minmax(0, 1.45fr) minmax(20rem, 0.75fr);
		border-bottom: 1px solid var(--border-trace);
	}

	.opening__copy {
		display: flex;
		flex-direction: column;
		justify-content: center;
		padding: 6rem clamp(2rem, 6vw, 6rem) 6rem 0;
	}

	.eyebrow,
	.section-label,
	.definition-list dt {
		margin: 0;
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.09em;
		line-height: 1.33;
		text-transform: uppercase;
	}

	.eyebrow {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		color: var(--time-marker);
	}

	.eyebrow::before {
		width: 1.75rem;
		height: 2px;
		background: currentColor;
		content: "";
	}

	h1 {
		max-width: 13ch;
		margin: 1.5rem 0 1.75rem;
		font-size: clamp(3rem, 6.5vw, 5.75rem);
		font-weight: 500;
		letter-spacing: -0.03em;
		line-height: 0.97;
	}

	.lede {
		max-width: 39rem;
		margin: 0;
		color: var(--marginal-note);
		font-family: var(--font-editorial);
		font-size: clamp(1.25rem, 2vw, 1.55rem);
		line-height: 1.42;
	}

	.opening__index {
		display: flex;
		flex-direction: column;
		justify-content: space-between;
		border-left: 1px solid var(--border-trace);
		padding: 3rem 0 3rem 3rem;
	}

	.index-heading {
		display: flex;
		justify-content: space-between;
		gap: 1rem;
		border-bottom: 1px solid var(--border-trace);
		padding-bottom: 1rem;
		color: var(--marginal-note);
		font-family: var(--font-record);
		font-size: 0.75rem;
		font-variant-numeric: tabular-nums;
		letter-spacing: 0.04em;
		text-transform: uppercase;
	}

	.index-statement {
		max-width: 16ch;
		margin: 2.25rem 0 3rem;
		font-family: var(--font-editorial);
		font-size: clamp(1.75rem, 3vw, 2.5rem);
		line-height: 1.14;
	}

	.index-sequence {
		position: relative;
		margin: 0;
		padding: 0;
		list-style: none;
	}

	.index-sequence::before {
		position: absolute;
		top: 0.5rem;
		bottom: 0.5rem;
		left: 5px;
		width: 1px;
		background: var(--border-trace);
		content: "";
	}

	.index-sequence li {
		position: relative;
		display: grid;
		grid-template-columns: 0.75rem minmax(0, 1fr);
		gap: 1rem;
		padding: 0.5625rem 0;
		color: var(--marginal-note);
		font-size: 0.75rem;
	}

	.sequence-point {
		position: relative;
		z-index: 1;
		width: 11px;
		height: 11px;
		margin-top: 0.25rem;
		border: 2px solid var(--reading-room);
		border-radius: 50%;
		background: var(--register-mark);
		box-shadow: 0 0 0 1px var(--register-mark);
	}

	.index-sequence__latest {
		color: var(--reading-ink) !important;
		font-weight: 500;
	}

	.index-sequence__latest .sequence-point {
		border-radius: var(--radius-mark);
		background: var(--time-marker);
		box-shadow: 0 0 0 1px var(--time-marker);
	}

	.index-empty {
		margin: 0;
		border-left: 2px solid var(--border-trace);
		padding-left: 1rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}

	.search-register {
		margin: 4rem 0 6rem;
		border-top: 2px solid var(--reading-ink);
		border-bottom: 1px solid var(--border-trace);
		background: var(--archive-layer);
	}

	.search-register__heading,
	.catalogue__heading {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto;
		align-items: end;
		gap: 1.5rem;
	}

	.search-register__heading {
		border-bottom: 1px solid var(--border-trace);
		padding: 2rem;
	}

	.search-register__heading h2,
	.catalogue__heading h2 {
		margin: 0.5rem 0 0;
		font-size: clamp(2rem, 4vw, 3.25rem);
		font-weight: 500;
		letter-spacing: -0.025em;
		line-height: 1.05;
	}

	.search-register__heading > p {
		max-width: 27rem;
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.9375rem;
	}

	.search-form {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto;
		align-items: end;
		gap: 0.75rem;
		padding: 2rem;
	}

	.search-form :global([data-input-control]) {
		background: var(--reading-room);
	}

	.search-form :global(.search-form__submit) {
		height: 3.5rem;
	}

	.search-examples {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.5rem 1rem;
		padding: 0 2rem 2rem;
	}

	.search-examples > span {
		color: var(--marginal-note);
		font-size: 0.8125rem;
	}

	.search-examples :global(.search-examples__action) {
		min-width: 0;
		padding-inline: 0;
	}

	.catalogue {
		padding-bottom: 6rem;
	}

	.catalogue__heading {
		border-bottom: 2px solid var(--reading-ink);
		padding-bottom: 1.5rem;
	}

	.result-status {
		margin: 0;
		color: var(--marginal-note);
		font-family: var(--font-record);
		font-size: 0.8125rem;
		font-variant-numeric: tabular-nums;
	}

	.catalogue__grid {
		display: grid;
		grid-template-columns: 15rem minmax(0, 1fr);
		gap: 0 3rem;
	}

	.record-list {
		margin: 0;
		padding: 0;
		list-style: none;
	}

	:global(.catalogue__pagination) {
		margin-top: 2rem;
	}

	.status-message {
		display: flex;
		align-items: flex-start;
		gap: 0.75rem;
		border-left: 3px solid currentColor;
		background: var(--archive-layer);
		padding: 1.25rem;
	}

	.catalogue__heading + .status-message {
		margin-top: 1.5rem;
	}

	.catalogue__grid .status-message {
		margin-top: 1.5rem;
	}

	.status-message--error {
		color: var(--time-marker);
	}

	.status-message--warning {
		color: var(--warning-ochre);
	}

	.status-message :global(svg) {
		width: 1.25rem;
		height: 1.25rem;
		flex: none;
		margin-top: 0.125rem;
		stroke-width: 1.75;
	}

	.status-message h3,
	.status-message p {
		margin: 0;
	}

	.status-message h3 {
		color: var(--reading-ink);
		font-size: 1rem;
		font-weight: 600;
	}

	.status-message p {
		margin-top: 0.25rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}

	.empty-register {
		display: grid;
		justify-items: start;
		padding: 4rem 0;
	}

	.empty-register__mark {
		position: relative;
		width: 5rem;
		height: 1rem;
		border-top: 1px solid var(--border-trace);
	}

	.empty-register__mark::before,
	.empty-register__mark::after,
	.empty-register__mark span {
		position: absolute;
		top: -4px;
		width: 7px;
		height: 7px;
		border: 1px solid var(--border-trace);
		border-radius: 50%;
		background: var(--reading-room);
		content: "";
	}

	.empty-register__mark::before {
		left: 0;
	}

	.empty-register__mark span {
		left: calc(50% - 3px);
	}

	.empty-register__mark::after {
		right: 0;
	}

	.empty-register h3 {
		margin: 1rem 0 0;
		font-family: var(--font-editorial);
		font-size: 1.75rem;
		font-weight: 500;
	}

	.empty-register p {
		max-width: 32rem;
		margin: 0.5rem 0 0.5rem;
		color: var(--marginal-note);
	}

	.principles {
		display: grid;
		grid-template-columns: minmax(0, 0.8fr) minmax(0, 1.2fr);
		gap: 6rem;
		border-top: 1px solid var(--border-trace);
		padding: 6rem 0;
	}

	.principles h2 {
		max-width: 12ch;
		margin: 0.75rem 0 0;
		font-size: clamp(2rem, 4vw, 3.25rem);
		font-weight: 500;
		letter-spacing: -0.025em;
		line-height: 1.05;
	}

	.principles__intro {
		max-width: 28rem;
		margin: 1.25rem 0 0;
		color: var(--marginal-note);
		font-family: var(--font-editorial);
		font-size: 1.25rem;
		line-height: 1.42;
	}

	.definition-list {
		margin: 0;
		border-top: 2px solid var(--reading-ink);
	}

	.definition-list > div {
		display: grid;
		grid-template-columns: 8rem minmax(0, 1fr);
		gap: 2rem;
		border-bottom: 1px solid var(--border-trace);
		padding: 1.5rem 0;
	}

	.definition-list dt {
		color: var(--time-marker);
	}

	.definition-list dd {
		margin: 0;
		color: var(--marginal-note);
		line-height: 1.55;
	}

	@media (max-width: 64rem) {
		.opening {
			grid-template-columns: minmax(0, 1.2fr) minmax(17rem, 0.8fr);
		}

		.opening__index {
			padding-left: 2rem;
		}
	}

	@media (max-width: 48rem) {
		.opening {
			min-height: 0;
			grid-template-columns: 1fr;
		}

		.opening__copy {
			padding: 4rem 0;
		}

		h1 {
			font-size: clamp(3rem, 14vw, 4.5rem);
		}

		.opening__index {
			border-top: 1px solid var(--border-trace);
			border-left: 0;
			padding: 2rem 0;
		}

		.index-statement {
			max-width: 22ch;
		}

		.search-register__heading,
		.catalogue__heading {
			grid-template-columns: 1fr;
			align-items: start;
		}

		.search-form {
			grid-template-columns: 1fr;
		}

		.search-form :global(.search-form__submit) {
			width: 100%;
		}

		.catalogue__grid {
			grid-template-columns: minmax(0, 1fr);
		}

		.principles {
			grid-template-columns: 1fr;
			gap: 2.5rem;
		}
	}

	@media (max-width: 34rem) {
		.search-register {
			margin-block: 3rem 4.5rem;
		}

		.search-register__heading,
		.search-form {
			padding: 1.5rem 1.25rem;
		}

		.search-examples {
			padding: 0 1.25rem 1.25rem;
		}

		.definition-list > div {
			grid-template-columns: 1fr;
			gap: 0.5rem;
		}
	}
</style>
