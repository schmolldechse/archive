<script lang="ts">
	import { afterNavigate, beforeNavigate, goto, invalidateAll } from "$app/navigation";
	import { navigating, page } from "$app/state";
	import type { SnapshotResponse } from "$api";
	import LocalTimestamp from "$lib/components/site/LocalTimestamp.svelte";
	import CaptureSequence from "$lib/components/snapshot-search/CaptureSequence.svelte";
	import SearchNotation from "$lib/components/snapshot-search/SearchNotation.svelte";
	import {
		draftFromState,
		emptyDraft,
		emptyState,
		searchHref,
		stateFromDraft,
		validateDraft,
		type SearchDraft,
		type SearchErrors,
		type SearchState
	} from "$lib/components/snapshot-search/search-state";
	import { ToastProvider, ToastViewport, createToastController } from "$lib/components/ui/toast";
	import { tick, untrack } from "svelte";
	import { MediaQuery } from "svelte/reactivity";
	import type { PageProps } from "./$types";

	let { data }: PageProps = $props();
	let draft = $state(untrack(() => draftFromState(data.state)));
	let errors = $state<SearchErrors>(untrack(() => data.errors));
	let selectedSnapshot = $state<SnapshotResponse | null>(null);
	let previewOpen = $state(false);
	let previewTrigger = $state<HTMLButtonElement | null>(null);
	const smallScreen = new MediaQuery("(width < 57.5rem)", false);
	const mobile = $derived(smallScreen.current);
	const pending = $derived(navigating.to !== null);
	const recentCaptures = $derived(data.results?.recentCaptureTimes ?? []);
	const registerNumber = $derived(String(data.results?.indexTotal ?? 0).padStart(3, "0"));
	const toast = createToastController();
	let lastTarget = untrack(() => page.url.pathname + page.url.search);
	let intent: { target: string; preserveDraft: boolean } | null = null;

	function scrollToResults(): void {
		const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
		document.getElementById("register-title")?.scrollIntoView({ behavior: reducedMotion ? "auto" : "smooth", block: "start" });
	}

	beforeNavigate(({ from, to }) => {
		if (to && from && (to.url.pathname !== from.url.pathname || to.url.search !== from.url.search)) {
			previewOpen = false;
			selectedSnapshot = null;
		}
	});
	afterNavigate(({ to, type }) => {
		const target = to?.url.pathname + (to?.url.search ?? "");
		const own = intent?.target === target && type !== "popstate";
		if (target !== lastTarget || type === "popstate") {
			if (!own || !intent?.preserveDraft) draft = draftFromState(data.state);
			errors = data.errors;
			previewOpen = false;
			selectedSnapshot = null;
		}
		if (own) requestAnimationFrame(scrollToResults);
		lastTarget = target;
		intent = null;
	});

	function changeDraft(patch: Partial<SearchDraft>): void {
		draft = { ...draft, ...patch };
		const remaining = { ...errors };
		for (const field of Object.keys(patch)) delete remaining[field as keyof SearchErrors];
		delete remaining.form;
		errors = remaining;
	}

	async function navigate(state: SearchState, preserveDraft = false): Promise<void> {
		const target = searchHref(state);
		if (!Object.keys(data.errors).length && target === page.url.pathname + page.url.search) {
			scrollToResults();
			return;
		}
		intent = { target, preserveDraft };
		try {
			await goto(target, { keepFocus: true, noScroll: true });
		} catch {
			intent = null;
			toast.add({ variant: "error", title: "The register could not be updated", description: "Please try the search again." });
		}
	}

	async function submit(nextDraft = draft): Promise<void> {
		errors = { ...validateDraft(nextDraft), ...(errors.order ? { order: errors.order } : {}) };
		if (Object.keys(errors).length) {
			await tick();
			document.querySelector<HTMLElement>('#browse [aria-invalid="true"], #snapshot-register [aria-invalid="true"]')?.focus();
			return;
		}
		previewOpen = false;
		selectedSnapshot = null;
		void navigate(stateFromDraft(nextDraft, data.state));
	}
	function resetFilters(): void {
		const nextDraft = { ...emptyDraft(), query: draft.query };
		draft = nextDraft;
		errors = errors.query ? { query: errors.query } : {};
		previewOpen = false;
		selectedSnapshot = null;
		void submit(nextDraft);
	}
	function clearAll(): void {
		draft = emptyDraft();
		errors = {};
		previewOpen = false;
		selectedSnapshot = null;
		void navigate(emptyState());
	}
	function choosePeriod(from: string, through: string): void {
		const current = draftFromState(data.state);
		const remove = current.from === from && current.through === through;
		const dates = { from: remove ? "" : from, through: remove ? "" : through };
		draft = { ...draft, ...dates };
		const next = stateFromDraft({ ...current, ...dates }, data.state);
		void navigate(next, true);
	}
	function choosePage(targetPage: number): void {
		// The Pagination primitive keeps native link navigation, including modified clicks.
		intent = { target: searchHref({ ...data.state, page: targetPage }), preserveDraft: true };
	}
	async function retry(): Promise<void> {
		await invalidateAll();
		errors = data.errors;
	}
	function inspectSnapshot(snapshot: SnapshotResponse, trigger: HTMLButtonElement): void {
		selectedSnapshot = snapshot;
		previewTrigger = trigger;
		previewOpen = true;
	}
</script>

<svelte:head>
	<title>{data.state.query ? `Search: ${data.state.query} — ARCHIV` : "ARCHIV — Public web record"}</title>
	<meta
		name="description"
		content="Search immutable, time-stamped captures of the public web with their source, context, and provenance intact."
	/>
</svelte:head>

<ToastProvider controller={toast}>
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
					<span>Capture times</span>
				</div>
				<p class="index-statement">A page is not one thing. It is every state we can prove.</p>
				{#if recentCaptures.length}
					<ol class="index-sequence">
						{#each recentCaptures as timestamp, index (index)}
							<li class:index-sequence__latest={index === 0}>
								<span class="sequence-point" aria-hidden="true"></span>
								<LocalTimestamp value={timestamp} />
							</li>
						{/each}
					</ol>
				{:else}
					<p class="index-empty">The register has no visible captures yet.</p>
				{/if}
			</aside>
		</section>

		<SearchNotation
			{draft}
			committed={data.state}
			{errors}
			{pending}
			onChange={changeDraft}
			onSubmit={() => {
				void submit();
			}}
			onReset={resetFilters}
		/>
		<CaptureSequence
			results={data.results}
			committed={data.state}
			snapshot={selectedSnapshot}
			{mobile}
			{previewOpen}
			{pending}
			error={data.loadError}
			invalid={Object.keys(data.errors).length > 0}
			onInspect={inspectSnapshot}
			onClear={clearAll}
			onRetry={() => {
				void retry();
			}}
			onPage={choosePage}
			onPeriod={choosePeriod}
			onPreviewOpenChange={(value) => {
				previewOpen = value;
			}}
			returnFocus={() => previewTrigger}
		/>

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

	<ToastViewport />
</ToastProvider>

<style>
	.main-shell {
		width: min(100%, 86rem);
		margin-inline: auto;
		padding-inline: clamp(1rem, 4vw, 4rem);
		overflow-wrap: anywhere;
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

		.principles {
			grid-template-columns: 1fr;
			gap: 2.5rem;
		}
	}

	@media (max-width: 34rem) {
		.definition-list > div {
			grid-template-columns: 1fr;
			gap: 0.5rem;
		}
	}
</style>
