<script lang="ts">
	import ArrowLeft from "@lucide/svelte/icons/arrow-left";
	import BrandMark from "$lib/components/site/BrandMark.svelte";
	import LocalTimestamp from "$lib/components/site/LocalTimestamp.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import type { PageProps } from "./$types";

	let { data }: PageProps = $props();
	const contentHref = $derived(`/api/snapshots/${data.snapshot.id}/content/index.html`);
</script>

<svelte:head>
	<title>{data.snapshot.title} — ARCHIV</title>
	<meta name="description" content={`Archived page captured by ARCHIV: ${data.snapshot.title}`} />
</svelte:head>

<div class="archived-page">
	<Button class="skip-link" variant="text" href="#main-content">Skip to archived page</Button>
	<header class="archive-header">
		<BrandMark variant="responsive" aria-label="ARCHIV home" />
		<div class="archive-header__record">
			<span>Preserved page</span>
			<LocalTimestamp value={data.snapshot.createdAt} />
		</div>
		<Button class="archive-header__back" variant="text" href={data.returnHref}
			><ArrowLeft size={18} aria-hidden="true" />Back to register</Button
		>
	</header>
	<main id="main-content" class="archived-page__content">
		<iframe
			src={contentHref}
			title={`Archived page: ${data.snapshot.title}`}
			sandbox=""
			referrerpolicy="no-referrer"
		></iframe>
	</main>
</div>

<style>
	.archived-page {
		display: flex;
		min-height: 100dvh;
		flex-direction: column;
	}
	.archive-header {
		display: flex;
		min-height: 5rem;
		align-items: center;
		justify-content: space-between;
		gap: 1.5rem;
		border-bottom: 1px solid var(--border-trace);
		padding: 0.75rem clamp(1rem, 4vw, 3rem);
	}
	.archive-header__record {
		display: flex;
		min-width: 0;
		align-items: baseline;
		gap: 0.75rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
	}
	.archive-header__record span {
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	.archive-header__record :global(time) {
		font-family: var(--font-record);
	}
	.archive-header :global(.archive-header__back) {
		font-size: 0.875rem;
		white-space: nowrap;
	}
	.archived-page__content {
		min-height: 35rem;
		flex: 1;
		background: var(--archive-frame);
	}
	iframe {
		display: block;
		width: 100%;
		height: calc(100dvh - 5rem);
		min-height: 35rem;
		border: 0;
		background: white;
	}
	.archived-page :global(.skip-link.button) {
		position: fixed;
		z-index: 10;
		top: 0.75rem;
		left: 0.75rem;
		transform: translateY(-160%);
		background: var(--register-mark);
		color: var(--reading-room);
	}
	.archived-page :global(.skip-link.button:focus) {
		transform: translateY(0);
	}
	@media (max-width: 42rem) {
		.archive-header {
			min-height: 4.5rem;
			gap: 0.75rem;
		}
		.archive-header__record {
			display: none;
		}
		.archive-header :global(.archive-header__back) {
			font-size: 0.8125rem;
		}
		iframe {
			height: calc(100dvh - 4.5rem);
		}
	}
</style>
