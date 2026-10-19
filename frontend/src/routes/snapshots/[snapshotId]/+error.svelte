<script lang="ts">
	import { page } from "$app/state";
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import RotateCcw from "@lucide/svelte/icons/rotate-ccw";
	import BrandMark from "$lib/components/site/BrandMark.svelte";
	import Button from "$lib/components/ui/Button.svelte";

	const canRetry = $derived(page.status >= 500);

	// A failed load has no snapshot data; retry only the current, same-origin GET.
	const retryHref = $derived(page.url.pathname + page.url.search);
</script>

<svelte:head>
	<title>An error occurred · Archive</title>
	<meta name="robots" content="noindex, nofollow" />
</svelte:head>

<main id="main-content" class="snapshot-error">
	<header class="snapshot-error__header">
		<BrandMark variant="primary" aria-label="Archive home" />
	</header>
	<div class="snapshot-error__content">
		<p class="snapshot-error__status" aria-label={`HTTP status ${page.status}`}>{page.status}</p>
		<section class="snapshot-error__message" aria-labelledby="snapshot-error-title">
			<h1 id="snapshot-error-title">An error occurred</h1>
			<p class="snapshot-error__description">This snapshot could not be loaded. Please try again later.</p>
			<div class="snapshot-error__actions">
				{#if canRetry}
					<Button href={retryHref} data-sveltekit-reload><RotateCcw size={18} aria-hidden="true" />Try again</Button>
					<Button variant="text" href="/">Browse snapshots<ArrowRight size={18} aria-hidden="true" /></Button>
				{:else}
					<Button href="/">Browse snapshots<ArrowRight size={18} aria-hidden="true" /></Button>
				{/if}
			</div>
		</section>
	</div>
</main>

<style>
	.snapshot-error {
		display: flex;
		min-height: 100svh;
		flex-direction: column;
		padding: 2rem clamp(1.25rem, 6vw, 4rem);
	}
	.snapshot-error__header {
		width: min(100%, 70rem);
		margin-inline: auto;
	}
	.snapshot-error__content {
		display: grid;
		width: min(100%, 70rem);
		flex: 1;
		grid-template-columns: minmax(0, 0.95fr) minmax(0, 1fr);
		align-content: center;
		align-items: center;
		gap: clamp(2rem, 6vw, 6rem);
		margin-inline: auto;
		padding-block: clamp(3rem, 10vh, 6rem);
	}
	.snapshot-error__message {
		border-inline-start: 1px solid var(--border-trace);
		padding: 1.5rem 0 1.5rem clamp(1.5rem, 3vw, 3rem);
	}
	.snapshot-error__status {
		margin: 0;
		color: var(--reading-ink);
		font-family: var(--font-record);
		font-size: clamp(9rem, 18vw, 17rem);
		font-weight: 400;
		line-height: 1;
		letter-spacing: -0.08em;
		font-variant-numeric: tabular-nums;
	}
	h1 {
		margin: 0;
		font-size: clamp(2rem, 3.5vw, 2.5rem);
		font-weight: 500;
		line-height: 1.12;
		letter-spacing: -0.025em;
		text-wrap: balance;
	}
	.snapshot-error__description {
		max-width: 38ch;
		margin: 1rem 0 0;
		color: var(--marginal-note);
		font-size: 1.125rem;
		line-height: 1.6;
	}
	.snapshot-error__actions {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.75rem 1rem;
		margin-top: 2rem;
	}
	@media (max-width: 48rem) {
		.snapshot-error__content {
			grid-template-columns: minmax(0, 1fr);
			gap: 2rem;
		}
		.snapshot-error__status {
			font-size: clamp(7rem, 30vw, 12rem);
		}
		.snapshot-error__message {
			border-inline-start: 0;
			border-top: 1px solid var(--border-trace);
			padding: 1.5rem 0 0;
		}
	}
	@media (max-width: 26rem) {
		.snapshot-error__actions {
			align-items: stretch;
			flex-direction: column;
		}
	}
</style>
