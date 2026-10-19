<script lang="ts">
	import type { SnapshotResponse } from "$api";
	import Badge from "$lib/components/ui/Badge.svelte";
	import ArchiveAttribution from "./ArchiveAttribution.svelte";
	import ArchiveCaptureTime from "./ArchiveCaptureTime.svelte";
	import SnapshotTags from "./SnapshotTags.svelte";
	import { formatBytes } from "./format";
	let { snapshot }: { snapshot: SnapshotResponse } = $props();
</script>

<div class="snapshot-metadata" data-component="snapshot-metadata">
	<div class="snapshot-metadata__provenance">
		<ArchiveCaptureTime value={snapshot.createdAt} />
		<ArchiveAttribution creator={snapshot.createdBy} />
	</div>
	{#if snapshot.description || snapshot.tags.length}
		<section class="snapshot-metadata__about" aria-label="About this page">
			<h4>About this page</h4>
			{#if snapshot.description}<p>{snapshot.description}</p>{/if}
			{#if snapshot.tags.length}
				<ul class="snapshot-tag-list" aria-label="Snapshot tags">
					{#each snapshot.tags as tag (tag)}<SnapshotTags {tag} />{/each}
				</ul>
			{/if}
		</section>
	{/if}
	{#if snapshot.quality !== null}
		<section class="snapshot-metadata__preservation" aria-label="Preservation">
			<h4>Preservation</h4>
			<dl>
				<div>
					<dt>Condition</dt>
					<dd>
						<Badge variant={snapshot.quality === "COMPLETE" ? "success" : "warning"} size="compact"
							>{snapshot.quality === "COMPLETE" ? "Complete" : "Incomplete"}</Badge
						>
					</dd>
				</div>
				<div>
					<dt>Resources</dt>
					<dd>{snapshot.resourceCount}</dd>
				</div>
				<div>
					<dt>Storage</dt>
					<dd>{formatBytes(Number(snapshot.storageBytes))}</dd>
				</div>
				<div>
					<dt>Capture duration</dt>
					<dd>{(Number(snapshot.durationMilliseconds) / 1000).toLocaleString("en", { maximumFractionDigits: 1 })} s</dd>
				</div>
			</dl>
		</section>
	{:else}
		<p>Capture in progress.</p>
	{/if}
</div>

<style>
	.snapshot-metadata {
		container-type: inline-size;
		min-width: 0;
	}
	.snapshot-metadata__provenance {
		display: grid;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		align-items: start;
		gap: 1.5rem;
		border-top: 2px solid var(--time-marker);
		padding-top: 1.5rem;
	}
	section {
		margin-top: 1.5rem;
		border-top: 1px solid var(--border-trace);
		padding-top: 1.5rem;
	}
	h4 {
		margin: 0 0 0.75rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		line-height: 1.33;
		text-transform: uppercase;
	}
	.snapshot-metadata__about p {
		margin: 0;
		font-size: 1rem;
		line-height: 1.6;
		overflow-wrap: anywhere;
	}
	dl {
		margin: 0;
	}
	dl > div {
		display: grid;
		grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
		align-items: center;
		gap: 1rem;
		border-bottom: 1px solid color-mix(in srgb, var(--border-trace) 55%, transparent);
		padding-block: 0.75rem;
	}
	dl > div:first-child {
		padding-top: 0;
	}
	dl > div:last-child {
		border-bottom: 0;
		padding-bottom: 0;
	}
	dt {
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
	dd {
		margin: 0;
		font-family: var(--font-record);
		font-size: 0.8125rem;
		text-align: right;
		overflow-wrap: anywhere;
	}
	@container (max-width: 28rem) {
		.snapshot-metadata__provenance {
			grid-template-columns: 1fr;
			gap: 1.25rem;
		}
	}
</style>
