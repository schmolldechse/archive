<script lang="ts">
	import ArrowLeft from "@lucide/svelte/icons/arrow-left";
	import BrandMark from "$lib/components/site/BrandMark.svelte";
	import SnapshotInformationDialog from "$lib/components/archive/SnapshotInformationDialog.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import type { PageProps } from "./$types";

	let { data }: PageProps = $props();
</script>

<svelte:head>
	<title>Snapshot: {data.snapshot.title} · Archive</title>
	<meta name="description" content={`Archived page captured by ARCHIV: ${data.snapshot.title}`} />
	{#if data.isPrivate}<meta name="robots" content="noindex, nofollow" />{/if}
</svelte:head>

<div class="archived-page">
	<Button class="skip-link" variant="text" href="#main-content">Skip to archived page</Button>
	<header class="archive-header">
		<BrandMark variant="responsive" aria-label="ARCHIV home" />
		<Button class="archive-header__back" variant="text" href={data.returnHref} aria-label="Back to register"
			><ArrowLeft size={18} aria-hidden="true" /><span
				>Back<span class="archive-header__back-destination">{" to register"}</span></span
			></Button
		>
	</header>
	<main id="main-content" class="archived-page__content">
		<div class="archived-page__information">
			<SnapshotInformationDialog snapshot={data.snapshot} history={data.history} historyUnavailable={!data.historyAvailable} />
		</div>
		<!-- Every snapshot runs on the opaque, credential-free snapshot origin. -->
		<iframe
			src={data.snapshot.contentUrl ?? undefined}
			title={`Archived page: ${data.snapshot.title}`}
			sandbox="allow-scripts"
			referrerpolicy="no-referrer"
		></iframe>
	</main>
</div>

<style>
	.archived-page {
		display: flex;
		height: 100dvh;
		min-height: 100dvh;
		flex-direction: column;
	}
	.archive-header {
		display: flex;
		flex-shrink: 0;
		min-height: 4.5rem;
		align-items: center;
		justify-content: space-between;
		gap: 1.5rem;
		border-bottom: 1px solid var(--border-trace);
		padding: 0.75rem clamp(1rem, 4vw, 3rem);
	}
	.archive-header :global(.archive-header__back) {
		font-size: 0.875rem;
		white-space: nowrap;
	}
	.archived-page__content {
		display: flex;
		min-height: 0;
		flex: 1;
		background: var(--archive-frame);
	}
	.archived-page__information {
		position: fixed;
		z-index: 5;
		right: calc(clamp(1rem, 2vw, 1.5rem) + env(safe-area-inset-right, 0px));
		bottom: calc(clamp(1rem, 2vw, 1.5rem) + env(safe-area-inset-bottom, 0px));
	}
	iframe {
		display: block;
		width: 100%;
		flex: 1;
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
			gap: 0.75rem;
		}
	}
	@media (max-width: 34rem) {
		.archive-header__back-destination {
			display: none;
		}
	}
</style>
