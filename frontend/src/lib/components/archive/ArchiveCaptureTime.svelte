<script lang="ts">
	import ArchiveTimestamp from "$lib/components/site/ArchiveTimestamp.svelte";
	let { value, layout = "default" }: { value: string; layout?: "default" | "compact" } = $props();
</script>

<div class="capture-time" data-component="archive-capture-time" data-layout={layout}>
	<p class="capture-time__label">Captured</p>
	<div class="capture-time__reading">
		<ArchiveTimestamp {value} class="capture-time__timestamp" />
	</div>
</div>

<style>
	.capture-time {
		min-width: 0;
	}
	.capture-time__label {
		margin: 0 0 0.5rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		line-height: 1rem;
		text-transform: uppercase;
	}
	.capture-time__reading {
		min-height: 1.5rem;
		color: var(--reading-ink);
	}
	.capture-time__reading :global(time) {
		font-size: 0.875rem;
		line-height: 1.5rem;
	}
	.capture-time[data-layout="compact"] .capture-time__label {
		margin-bottom: 0.25rem;
	}
	.capture-time[data-layout="compact"] .capture-time__reading :global(time) {
		font-size: 0.8125rem;
	}
</style>
