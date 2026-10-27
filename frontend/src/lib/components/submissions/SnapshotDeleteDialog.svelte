<script lang="ts">
	import { goto } from "$app/navigation";
	import type { ProcessingStatus } from "$api";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import Trash2 from "@lucide/svelte/icons/trash-2";
	import { tick } from "svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import {
		DialogActions,
		DialogClose,
		DialogContent,
		DialogDescription,
		DialogRoot,
		DialogTitle
	} from "$lib/components/ui/dialog";

	let {
		endpoint,
		title,
		sourceUrl,
		processingStatus,
		etag,
		admin
	}: {
		endpoint: string;
		title: string;
		sourceUrl: string | null;
		processingStatus: ProcessingStatus;
		etag: string;
		admin: boolean;
	} = $props();

	let open = $state(false);
	let deleting = $state(false);
	let message = $state("");
	let stale = $state(false);
	let trigger = $state<HTMLButtonElement | HTMLAnchorElement | null>(null);
	let cancel = $state<HTMLButtonElement | null>(null);
	let errorNotice = $state<HTMLDivElement | null>(null);
	const backHref = $derived(admin ? "/admin/snapshots" : "/account/submissions");
	const processingNotice = $derived.by(() => {
		if (processingStatus === "RUNNING") return "The active capture will be stopped.";
		if (processingStatus === "QUEUED") return "The queued capture will be cancelled.";
		return "";
	});

	function showDialog() {
		message = "";
		stale = false;
		open = true;
	}

	async function deleteSnapshot() {
		if (deleting) return;
		deleting = true;
		message = "";
		stale = false;
		try {
			const response = await fetch(endpoint, {
				method: "DELETE",
				credentials: "include",
				cache: "no-store",
				redirect: "error",
				headers: { "If-Match": etag }
			});
			if (response.status === 204) {
				await goto(backHref, { replaceState: true, invalidateAll: true });
				return;
			}
			if (response.status === 404) {
				await goto(backHref, { replaceState: true, invalidateAll: true });
				return;
			}
			if (response.status === 412) {
				stale = true;
				message = "This snapshot has changed. Reload it before permanently deleting it.";
			} else if (response.status === 401 || response.status === 403) {
				message = admin
					? "Your snapshot administration access has changed. Return to the snapshot list or sign in again."
					: "Your access to this snapshot has changed. Return to your submissions or sign in again.";
			} else {
				const problem = await response.json().catch(() => null);
				message = problem?.detail ?? "The snapshot could not be deleted. Please try again.";
			}
		} catch {
			message = "The submission service is temporarily unavailable. Please try again.";
		} finally {
			deleting = false;
			await tick();
			errorNotice?.focus();
		}
	}
</script>

<Button bind:ref={trigger} variant="critical" onclick={showDialog}>
	<Trash2 aria-hidden="true" />Delete snapshot
</Button>

<DialogRoot bind:open>
	<DialogContent
		size="small"
		closeOnEscape={!deleting}
		closeOnOutsidePointer={!deleting}
		initialFocus={() => cancel}
		returnFocus={() => (trigger instanceof HTMLButtonElement ? trigger : null)}
	>
		<DialogTitle>Delete snapshot?</DialogTitle>
		<DialogDescription>
			This permanently deletes the snapshot record, history, archived page, screenshot, and stored resources. This action cannot
			be undone.
		</DialogDescription>
		<div class="delete-target">
			<p class="delete-target__label">Snapshot</p>
			<p class="delete-target__title">{title}</p>
			{#if sourceUrl}<p class="delete-target__source">{sourceUrl}</p>{/if}
		</div>
		{#if processingNotice}<p class="processing-notice">{processingNotice}</p>{/if}
		{#if message}
			<div bind:this={errorNotice} class="delete-error" role="alert" tabindex="-1">
				<CircleAlert aria-hidden="true" />
				<div>
					<p>{message}</p>
					{#if stale}<Button onclick={() => globalThis.location.reload()}>Reload snapshot</Button>{/if}
				</div>
			</div>
		{/if}
		<DialogActions>
			<DialogClose bind:ref={cancel} disabled={deleting}>Cancel</DialogClose>
			<Button variant="critical" disabled={stale} loading={deleting} loadingLabel="Deleting snapshot…" onclick={deleteSnapshot}>
				<Trash2 aria-hidden="true" />Delete permanently
			</Button>
		</DialogActions>
	</DialogContent>
</DialogRoot>

<style>
	.delete-target {
		margin-block-start: 1.5rem;
		border-inline-start: 3px solid var(--time-marker);
		background: var(--archive-layer);
		padding: 1rem;
	}

	.delete-target p,
	.delete-error p {
		margin: 0;
	}

	.delete-target__label {
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}

	.delete-target__title {
		margin-block-start: 0.375rem !important;
		font-family: var(--font-editorial);
		font-size: 1.25rem;
		font-weight: 500;
		line-height: 1.25;
		overflow-wrap: anywhere;
	}

	.delete-target__source {
		margin-block-start: 0.5rem !important;
		color: var(--marginal-note);
		font-family: var(--font-record);
		font-size: 0.75rem;
		line-height: 1.45;
		overflow-wrap: anywhere;
	}

	.processing-notice {
		margin-block-start: 1rem;
		color: var(--marginal-note);
		font-size: 0.9375rem;
	}

	.delete-error {
		display: flex;
		gap: 0.75rem;
		margin-block-start: 1rem;
		border: 1px solid var(--time-marker);
		border-radius: var(--radius-control);
		padding: 0.875rem;
	}

	.delete-error > :global(svg) {
		width: 1.25rem;
		height: 1.25rem;
		flex: none;
		margin-block-start: 0.125rem;
		color: var(--time-marker);
	}

	.delete-error :global(.button) {
		margin-block-start: 0.75rem;
	}

	@media (max-width: 36rem) {
		:global([data-dialog-actions]) :global(.button) {
			width: 100%;
		}
	}
</style>
