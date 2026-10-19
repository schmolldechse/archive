<script lang="ts">
	import { onMount, onDestroy } from "svelte";
	import { apiUrl } from "$lib/api-url";
	import Button from "$lib/components/ui/Button.svelte";
	import ImageOff from "@lucide/svelte/icons/image-off";
	let { path, title }: { path: string; title: string } = $props();
	let source = $state<string | null>(null);
	let loading = $state(true);
	let failed = $state(false);
	const controller = new AbortController();
	let objectUrl: string | null = null;
	/** Authenticated fetch keeps private images out of public resource URLs and shared caches. */
	async function loadPreview() {
		loading = true;
		failed = false;
		try {
			const response = await fetch(apiUrl(path), { credentials: "include", cache: "no-store", signal: controller.signal });
			if (!response.ok) throw new Error("Preview unavailable");
			const blob = await response.blob();
			if (controller.signal.aborted) return;
			if (objectUrl) URL.revokeObjectURL(objectUrl);
			objectUrl = URL.createObjectURL(blob);
			source = objectUrl;
		} catch {
			if (!controller.signal.aborted) failed = true;
		} finally {
			if (!controller.signal.aborted) loading = false;
		}
	}
	onMount(() => {
		void loadPreview();
	});
	onDestroy(() => {
		controller.abort();
		if (objectUrl) URL.revokeObjectURL(objectUrl);
	});
</script>

<section class="private-preview" aria-label="Private capture preview" aria-busy={loading}>
	{#if loading}<p role="status">Loading preview…</p>
	{:else if failed}<div class="preview-error">
			<ImageOff aria-hidden="true" />
			<h2>Preview unavailable</h2>
			<p>The screenshot could not be loaded. The snapshot details remain available.</p>
			<Button onclick={loadPreview}>Retry preview</Button>
		</div>
	{:else if source}<img src={source} alt={`Captured page: ${title}`} onerror={() => (failed = true)} />{/if}
</section>

<style>
	.private-preview {
		display: grid;
		align-items: center;
		justify-items: center;
		min-height: 12rem;
		padding: 1rem;
		background: var(--archive-frame);
		border-radius: var(--radius-control);
	}
	img {
		display: block;
		max-width: 100%;
		max-height: 34rem;
		object-fit: contain;
		background: white;
	}
	.private-preview > p {
		padding: 1rem;
		background: var(--reading-room);
		color: var(--reading-ink);
	}
	.preview-error {
		padding: 1.5rem;
		background: var(--reading-room);
		max-width: 34rem;
	}
	.preview-error > :global(svg) {
		color: var(--marginal-note);
	}
	h2 {
		margin: 0.75rem 0;
		font: 600 1.125rem/1.4 var(--font-interface);
	}
	.preview-error p {
		color: var(--marginal-note);
	}
</style>
