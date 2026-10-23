<script lang="ts">
	import { goto, invalidateAll } from "$app/navigation";
	import { tick, untrack } from "svelte";
	import type { SnapshotSubmissionDetailResponse } from "$api";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import Breadcrumb from "$lib/components/ui/breadcrumb/Breadcrumb.svelte";
	import BreadcrumbItem from "$lib/components/ui/breadcrumb/BreadcrumbItem.svelte";
	import BreadcrumbLink from "$lib/components/ui/breadcrumb/BreadcrumbLink.svelte";
	import BreadcrumbList from "$lib/components/ui/breadcrumb/BreadcrumbList.svelte";
	import BreadcrumbPage from "$lib/components/ui/breadcrumb/BreadcrumbPage.svelte";
	import BreadcrumbSeparator from "$lib/components/ui/breadcrumb/BreadcrumbSeparator.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import SnapshotEditForm from "./SnapshotEditForm.svelte";
	import { createSnapshotEditDraft, type SnapshotEditDraft } from "./snapshot-edit-draft";

	let {
		data
	}: {
		data: {
			detail: SnapshotSubmissionDetailResponse;
			etag: string;
			admin: boolean;
		};
	} = $props();
	let draft = $state<SnapshotEditDraft>(untrack(() => createSnapshotEditDraft(data.detail)));
	let reloading = $state(false);
	let conflict = $state(false);
	let message = $state("");
	let errorNotice = $state<HTMLElement | null>(null);
	const detailHref = $derived(`${data.admin ? "/admin/snapshots" : "/account/submissions"}/${data.detail.snapshot.id}`);
	const listHref = $derived(data.admin ? "/admin/snapshots" : "/account/submissions");

	/** Returns to the record after a successful edit so its updated history is immediately visible. */
	async function saved(): Promise<void> {
		await goto(detailHref);
	}

	/** Keeps an ETag conflict recoverable by refreshing the record before another save. */
	async function reloadAfterConflict(): Promise<void> {
		reloading = true;
		message = "";
		try {
			await invalidateAll();
			draft = createSnapshotEditDraft(data.detail);
			conflict = false;
		} catch {
			message = "The current snapshot could not be reloaded. Please try again.";
		} finally {
			reloading = false;
		}
	}

	/** Moves focus to the page-level conflict message when the form reports a stale ETag. */
	async function reportConflict(): Promise<void> {
		conflict = true;
		message = "This snapshot changed while you were editing it. Reload it before saving changes.";
		await tick();
		errorNotice?.focus();
	}
</script>

<svelte:head>
	<title>Edit {data.detail.snapshot.title} · {data.admin ? "Snapshot record" : "My submissions"} · Archive</title>
	<meta name="robots" content="noindex, nofollow" />
</svelte:head>

<main id="main-content" class="snapshot-edit-page">
	<Breadcrumb aria-label="Snapshot editing path" class="page-breadcrumb">
		<BreadcrumbList>
			<BreadcrumbItem
				><BreadcrumbLink href={listHref}>{data.admin ? "All snapshots" : "My submissions"}</BreadcrumbLink></BreadcrumbItem
			>
			<BreadcrumbSeparator />
			<BreadcrumbItem><BreadcrumbLink href={detailHref}>{data.detail.snapshot.title}</BreadcrumbLink></BreadcrumbItem>
			<BreadcrumbSeparator />
			<BreadcrumbItem><BreadcrumbPage>Edit snapshot</BreadcrumbPage></BreadcrumbItem>
		</BreadcrumbList>
	</Breadcrumb>

	<header class="edit-heading">
		<p class="section-label">{data.admin ? "Snapshot record" : "Your submission"}</p>
		<h1>{data.detail.snapshot.title}</h1>
		<p class="edit-intro">Update the details associated with this archived record.</p>
	</header>

	{#if message}
		<div bind:this={errorNotice} class="edit-message" tabindex="-1" role="alert">
			<CircleAlert aria-hidden="true" />
			<div>
				<p>{message}</p>
				{#if conflict}<Button onclick={reloadAfterConflict} loading={reloading}>Reload snapshot</Button>{/if}
			</div>
		</div>
	{/if}

	{#key data.etag}
		<SnapshotEditForm
			detail={data.detail}
			etag={data.etag}
			bind:draft
			admin={data.admin}
			disabled={conflict || reloading}
			onCancel={() => goto(detailHref)}
			onSaved={saved}
			onConflict={reportConflict}
		/>
	{/key}
</main>

<style>
	.snapshot-edit-page {
		width: min(100%, 78rem);
		min-height: 65vh;
		margin-inline: auto;
		padding: 1.25rem clamp(1rem, 4vw, 3rem) 4rem;
	}
	:global(.page-breadcrumb) {
		margin-bottom: 2rem;
	}
	.edit-heading {
		padding-bottom: 1.5rem;
	}
	.section-label {
		margin: 0 0 0.75rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	h1 {
		margin: 0;
		overflow-wrap: anywhere;
		font: 500 clamp(2rem, 4vw, 3rem)/1.15 var(--font-editorial);
		letter-spacing: -0.02em;
	}
	.edit-intro {
		max-width: 48ch;
		margin: 0.75rem 0 0;
		color: var(--marginal-note);
		font-size: 0.9375rem;
	}
	.edit-message {
		display: flex;
		align-items: flex-start;
		gap: 0.75rem;
		margin-bottom: 1.5rem;
		padding: 1rem;
		border: 1px solid var(--time-marker);
	}
	.edit-message > :global(svg) {
		flex: none;
		color: var(--time-marker);
	}
	.edit-message p {
		margin: 0 0 0.75rem;
	}
	@media (max-width: 40rem) {
		:global(.page-breadcrumb) {
			margin-bottom: 1.5rem;
		}
	}
</style>
