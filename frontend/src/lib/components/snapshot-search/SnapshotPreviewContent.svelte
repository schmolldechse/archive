<script lang="ts">
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import ExternalLink from "@lucide/svelte/icons/external-link";
	import type { SnapshotResponse } from "$api";
	import LocalTimestamp from "$lib/components/site/LocalTimestamp.svelte";
	import Badge from "$lib/components/ui/Badge.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import { formatBytes } from "$lib/components/archive/format";
	let { snapshot, onCopy }: { snapshot: SnapshotResponse | null; onCopy: (snapshot: SnapshotResponse) => void } = $props();
	let failedImage = $state<string | null>(null);
	const source = $derived(snapshot?.sourceUrl ?? snapshot?.originalLink);
</script>

<div class="preview-content" data-component="snapshot-preview-content">
	{#if snapshot}
		<div class="preview-register">
			<p class="label">Selected preserved state</p>
			<LocalTimestamp value={snapshot.createdAt} />
		</div>
		<div class="archive-frame">
			{#if failedImage === snapshot.screenshotUrl}
				<div class="image-error">
					<CircleAlert aria-hidden="true" />
					<h4>Preview image unavailable</h4>
					<p>The image could not be loaded. The record and archived page remain available.</p>
				</div>
			{:else}
				<img
					src={snapshot.screenshotUrl}
					alt={`Archived screenshot of ${snapshot.title}`}
					onerror={(event) => {
						const image = event.currentTarget;
						if (image instanceof HTMLImageElement) failedImage = image.currentSrc || image.src;
					}}
				/>
			{/if}
		</div>
		<div class="preview-details">
			<h3>{snapshot.title}</h3>
			{#if source}<a class="preview-source" href={source} target="_blank" rel="noreferrer">{source}</a>{:else}<p
					class="preview-source"
				>
					Uploaded HTML document
				</p>{/if}
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
					<dt>Record ID</dt>
					<dd>{snapshot.id}</dd>
				</div>
			</dl>
			<div class="preview-actions">
				<Button href={snapshot.contentUrl} target="_blank" rel="noreferrer" variant="primary"
					>Open archived page<ExternalLink aria-hidden="true" /></Button
				>
				{#if source}<Button
						onclick={() => {
							if (snapshot) onCopy(snapshot);
						}}>Copy source</Button
					>{/if}
			</div>
		</div>
	{:else}
		<div class="preview-empty">
			<div class="empty-register" aria-hidden="true"></div>
			<h3>No snapshot selected</h3>
			<p>Choose “Inspect snapshot” beside a record to view its preserved state.</p>
		</div>
	{/if}
</div>

<style>
	.preview-register {
		border-bottom: 1px solid var(--border-trace);
		padding: 1rem;
	}
	.label {
		color: var(--marginal-note);
		font-size: 0.75rem;
		letter-spacing: 0.08em;
		text-transform: uppercase;
		margin: 0 0 0.5rem;
	}
	.preview-register :global(time) {
		font-size: 0.75rem;
	}
	.archive-frame {
		background: var(--archive-frame);
		padding: 0.5rem;
		max-height: 24rem;
		overflow-y: auto;
	}
	.archive-frame img {
		display: block;
		width: 100%;
		height: auto;
	}
	.preview-details {
		padding: 1.5rem 1rem;
	}
	h3 {
		font-family: var(--font-editorial);
		font-size: 1.5rem;
		font-weight: 500;
		line-height: 1.25;
		margin: 0 0 0.75rem;
	}
	.preview-source {
		display: block;
		font-family: var(--font-record);
		font-size: 0.8125rem;
		overflow-wrap: anywhere;
		margin: 0;
	}
	dl {
		margin-block: 1.5rem;
	}
	dl > div {
		display: grid;
		grid-template-columns: auto minmax(0, 1fr);
		align-items: start;
		gap: 1rem;
		padding-block: 0.75rem;
		border-top: 1px solid var(--border-trace);
	}
	dt {
		font-size: 0.75rem;
		color: var(--marginal-note);
	}
	dd {
		margin: 0;
		font-family: var(--font-record);
		font-size: 0.75rem;
		text-align: right;
		overflow-wrap: anywhere;
	}
	.preview-actions {
		display: grid;
		gap: 0.5rem;
	}
	.preview-actions :global(a),
	.preview-actions :global(button) {
		width: 100%;
	}
	.preview-empty {
		padding: 3rem 1.5rem;
	}
	.preview-empty p,
	.image-error p {
		color: var(--marginal-note);
		font-size: 0.9375rem;
	}
	.empty-register {
		width: 5rem;
		height: 3rem;
		margin-bottom: 2rem;
		border-block: 1px solid var(--border-trace);
		border-left: 3px solid var(--border-trace);
	}
	.image-error {
		background: var(--reading-room);
		padding: 1rem;
	}
	.image-error :global(svg) {
		width: 1.5rem;
		color: var(--time-marker);
	}
	.image-error h4 {
		font-size: 1rem;
		font-weight: 600;
	}
</style>
