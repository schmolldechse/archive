<script lang="ts">
	import Check from "@lucide/svelte/icons/check";
	import { env } from "$env/dynamic/public";
	import ArchivePageDialog from "$lib/components/site/ArchivePageDialog.svelte";
	import SiteFooter from "$lib/components/site/SiteFooter.svelte";
	import SiteHeader from "$lib/components/site/SiteHeader.svelte";
	import type { LayoutProps } from "./$types";
	import { onDestroy } from "svelte";

	let { children }: LayoutProps = $props();
	let archiveDialogOpen = $state(false);
	let notice = $state<string | null>(null);
	let noticeTimer: ReturnType<typeof setTimeout> | undefined;

	const healthUrl = `${(env.PUBLIC_API_BASE_URL || "http://localhost:5200").replace(/\/$/, "")}/healthz`;

	function showSubmissionNotice(): void {
		if (noticeTimer) window.clearTimeout(noticeTimer);

		notice = "The submission route is outside this homepage implementation.";
		noticeTimer = window.setTimeout(() => (notice = null), 3200);
	}

	onDestroy(() => {
		if (noticeTimer) window.clearTimeout(noticeTimer);
	});
</script>

<a class="skip-link" href="#main-content">Skip to the archive</a>

{#if notice}
	<div class="route-notice" role="status" aria-live="polite">
		<Check aria-hidden="true" />
		<span>{notice}</span>
	</div>
{/if}

<SiteHeader onArchivePage={() => (archiveDialogOpen = true)} />
{@render children()}
<SiteFooter {healthUrl} onArchivePage={() => (archiveDialogOpen = true)} />
<ArchivePageDialog bind:open={archiveDialogOpen} onContinue={showSubmissionNotice} />

<style>
	.skip-link {
		position: fixed;
		z-index: 100;
		top: 0.75rem;
		left: 0.75rem;
		transform: translateY(-160%);
		border-radius: var(--radius-control);
		background: var(--register-mark);
		padding: 0.625rem 0.875rem;
		color: var(--reading-room);
		font-weight: 600;
	}

	.skip-link:focus {
		transform: translateY(0);
	}

	.route-notice {
		position: fixed;
		z-index: 60;
		top: 1rem;
		left: 50%;
		display: flex;
		max-width: calc(100vw - 2rem);
		align-items: center;
		gap: 0.625rem;
		transform: translateX(-50%);
		border: 1px solid var(--preservation-green);
		border-radius: var(--radius-control);
		background: var(--reading-room);
		box-shadow: var(--shadow-overlay);
		padding: 0.75rem 1rem;
		font-size: 0.875rem;
		font-weight: 600;
	}

	.route-notice :global(svg) {
		width: 1.125rem;
		height: 1.125rem;
		flex: none;
		color: var(--preservation-green);
		stroke-width: 2;
	}
</style>
