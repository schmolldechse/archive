<script lang="ts">
	import { page } from "$app/state";
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import RotateCcw from "@lucide/svelte/icons/rotate-ccw";
	import Button from "$lib/components/ui/Button.svelte";

	let { standalone = false }: { standalone?: boolean } = $props();
	const canRetry = $derived(page.status >= 500);

	// Error boundaries cannot depend on data from the failed page or layout.
	const retryHref = $derived(page.url.pathname + page.url.search);
</script>

<svelte:head>
	<title>An error occurred · Archive</title>
	<meta name="robots" content="noindex, nofollow" />
</svelte:head>

<main id="main-content" class="page-error" class:page-error--standalone={standalone} data-component="page-error">
	<p class="page-error__status" aria-label={`HTTP status ${page.status}`}>{page.status}</p>
	<section class="page-error__message" aria-labelledby="page-error-title">
		<h1 id="page-error-title">An error occurred</h1>
		<p class="page-error__description">This page could not be loaded. Please try again later.</p>
		<div class="page-error__actions">
			{#if canRetry}
				<Button href={retryHref} data-sveltekit-reload><RotateCcw size={18} aria-hidden="true" />Try again</Button>
				<Button variant="text" href="/">Back to archive<ArrowRight size={18} aria-hidden="true" /></Button>
			{:else}
				<Button href="/">Back to archive<ArrowRight size={18} aria-hidden="true" /></Button>
			{/if}
		</div>
	</section>
</main>

<style>
	.page-error {
		display: grid;
		width: min(100%, 78rem);
		min-height: 65svh;
		grid-template-columns: minmax(0, 0.95fr) minmax(0, 1fr);
		align-content: center;
		align-items: center;
		gap: clamp(2rem, 6vw, 6rem);
		margin-inline: auto;
		padding: clamp(3rem, 10vh, 6rem) clamp(1.25rem, 6vw, 4rem);
	}
	.page-error--standalone {
		min-height: 0;
		flex: 1;
	}
	.page-error__status {
		margin: 0;
		color: var(--reading-ink);
		font-family: var(--font-record);
		font-size: clamp(9rem, 18vw, 17rem);
		font-weight: 400;
		line-height: 1;
		letter-spacing: -0.08em;
		font-variant-numeric: tabular-nums;
	}
	.page-error__message {
		border-inline-start: 1px solid var(--border-trace);
		padding: 1.5rem 0 1.5rem clamp(1.5rem, 3vw, 3rem);
	}
	h1 {
		margin: 0;
		font-size: clamp(2rem, 3.5vw, 2.5rem);
		font-weight: 500;
		line-height: 1.12;
		letter-spacing: -0.025em;
		text-wrap: balance;
	}
	.page-error__description {
		max-width: 38ch;
		margin: 1rem 0 0;
		color: var(--marginal-note);
		font-size: 1.125rem;
		line-height: 1.6;
	}
	.page-error__actions {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.75rem 1rem;
		margin-top: 2rem;
	}
	@media (max-width: 48rem) {
		.page-error {
			grid-template-columns: minmax(0, 1fr);
			gap: 2rem;
		}
		.page-error__status {
			font-size: clamp(7rem, 30vw, 12rem);
		}
		.page-error__message {
			border-inline-start: 0;
			border-top: 1px solid var(--border-trace);
			padding: 1.5rem 0 0;
		}
	}
	@media (max-width: 26rem) {
		.page-error__actions {
			align-items: stretch;
			flex-direction: column;
		}
	}
</style>
