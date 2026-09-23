<script lang="ts">
	import Check from "@lucide/svelte/icons/check";
	import ExternalLink from "@lucide/svelte/icons/external-link";
	import TriangleAlert from "@lucide/svelte/icons/triangle-alert";
	import X from "@lucide/svelte/icons/x";

	import type { SnapshotResponse } from "$api";
	import { uploadSourceLabel } from "$lib/components/archive/format";
	import LocalTimestamp from "$lib/components/site/LocalTimestamp.svelte";
	import Badge from "$lib/components/ui/Badge.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import { DialogClose, DialogContent, DialogRoot, DialogTitle } from "$lib/components/ui/dialog";

	interface SnapshotPreviewProps {
		open?: boolean;
		snapshot: SnapshotResponse | null;
		returnFocus?: () => HTMLElement | null;
	}

	let { open = $bindable(false), snapshot, returnFocus }: SnapshotPreviewProps = $props();
	const source = $derived(snapshot?.sourceUrl ?? snapshot?.originalLink ??
		(snapshot ? uploadSourceLabel(snapshot.sourceType) : ""));
</script>

<DialogRoot bind:open>
	{#if snapshot}
		<DialogContent id="snapshot-preview" size="viewport" class="snapshot-preview" aria-describedby={undefined} {returnFocus}>
			<div class="snapshot-preview__banner">
				<div class="snapshot-preview__source">
					<span class="snapshot-preview__label">Archived source</span>
					{#if snapshot.sourceUrl || snapshot.originalLink}
						<a href={source} target="_blank" rel="noreferrer">{source}</a>
					{:else}
						<span>{source}</span>
					{/if}
				</div>

				<div class="snapshot-preview__capture">
					<span class="snapshot-preview__label">Captured</span>
					<LocalTimestamp value={snapshot.createdAt} />
					<Badge variant={snapshot.quality === "COMPLETE" ? "success" : "warning"} size="compact">
						{#snippet icon()}
							{#if snapshot.quality === "COMPLETE"}
								<Check aria-hidden="true" />
							{:else}
								<TriangleAlert aria-hidden="true" />
							{/if}
						{/snippet}
						{snapshot.quality === "COMPLETE" ? "Integrity recorded" : "Partial capture recorded"}
					</Badge>
				</div>

				<DialogClose class="snapshot-preview__close" aria-label="Close snapshot preview">
					<X aria-hidden="true" />
				</DialogClose>
			</div>

			<div class="snapshot-preview__heading">
				<div>
					<p>Snapshot preview</p>
					<DialogTitle>{snapshot.title}</DialogTitle>
				</div>
				<Button href={snapshot.contentUrl} target="_blank" rel="noreferrer" variant="secondary">
					Open archived page
					<ExternalLink aria-hidden="true" />
				</Button>
			</div>

			<div class="snapshot-preview__frame">
				<img src={snapshot.screenshotUrl} alt={`Full-page capture of ${snapshot.title}`} />
			</div>
		</DialogContent>
	{/if}
</DialogRoot>

<style>
	:global(.snapshot-preview) {
		display: flex;
		flex-direction: column;
		padding: 0;
		overflow: hidden;
	}

	.snapshot-preview__banner {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto auto;
		align-items: center;
		gap: 2rem;
		border-bottom: 1px solid var(--border-trace);
		padding: 1rem 1.5rem;
		background: var(--archive-layer);
	}

	.snapshot-preview__source,
	.snapshot-preview__capture {
		display: grid;
		min-width: 0;
		gap: 0.25rem;
	}

	.snapshot-preview__source > a,
	.snapshot-preview__source > span:last-child,
	.snapshot-preview__capture :global(time) {
		font-family: var(--font-record);
		font-size: 0.75rem;
		line-height: 1.45;
		overflow-wrap: anywhere;
	}

	.snapshot-preview__label,
	.snapshot-preview__heading p {
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.6875rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}

	.snapshot-preview__capture :global(.badge) {
		width: fit-content;
	}

	.snapshot-preview__banner :global(.snapshot-preview__close) {
		width: 2.75rem;
		padding-inline: 0;
	}

	.snapshot-preview__heading {
		display: flex;
		align-items: end;
		justify-content: space-between;
		gap: 2rem;
		border-bottom: 1px solid var(--border-trace);
		padding: 1.5rem;
	}

	.snapshot-preview__heading :global([data-dialog-title]) {
		margin-top: 0.375rem;
	}

	.snapshot-preview__frame {
		min-height: 0;
		flex: 1;
		overflow: auto;
		background: var(--archive-frame);
		padding: clamp(0.5rem, 4vw, 3rem);
	}

	.snapshot-preview__frame img {
		display: block;
		width: min(100%, 80rem);
		height: auto;
		margin-inline: auto;
		border: 1px solid rgb(24 32 31 / 32%);
		background: white;
	}

	@media (max-width: 48rem) {
		.snapshot-preview__banner {
			grid-template-columns: minmax(0, 1fr) auto;
			gap: 0.875rem;
		}

		.snapshot-preview__capture {
			grid-column: 1;
		}

		.snapshot-preview__banner :global(.snapshot-preview__close) {
			grid-row: 1 / span 2;
			grid-column: 2;
		}

		.snapshot-preview__heading {
			align-items: stretch;
			flex-direction: column;
			gap: 1rem;
		}
	}
</style>
