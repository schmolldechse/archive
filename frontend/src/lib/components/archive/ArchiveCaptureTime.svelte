<script lang="ts">
	import LocalTimestamp from "$lib/components/site/LocalTimestamp.svelte";
	let { value, layout = "default" }: { value: string; layout?: "default" | "compact" } = $props();
</script>

<div class="capture-time" data-component="archive-capture-time" data-layout={layout}>
	<p class="capture-time__label">Captured</p>
	<div class="capture-time__reading">
		<div class="capture-time__date">
			<LocalTimestamp {value} format="day" class="capture-time__day" />
			<LocalTimestamp {value} format="month-year" class="capture-time__month" />
		</div>
		<LocalTimestamp {value} format="time" class="capture-time__clock" />
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
		display: grid;
		min-height: 2.75rem;
		align-content: start;
		color: var(--reading-ink);
	}
	.capture-time__date {
		display: flex;
		flex-wrap: wrap;
		align-items: baseline;
		column-gap: 0.5ch;
		font-family: var(--font-record);
		font-size: 1.125rem;
	}
	.capture-time__date :global(time) {
		font-size: inherit;
		font-weight: 500;
		line-height: 1.5rem;
	}
	.capture-time__reading :global(.capture-time__clock) {
		color: var(--marginal-note);
		font-size: 0.8125rem;
		line-height: 1.25rem;
	}
	.capture-time[data-layout="compact"] .capture-time__label {
		margin-bottom: 0.25rem;
	}
	.capture-time[data-layout="compact"] .capture-time__reading {
		min-height: 0;
		grid-template-columns: auto auto;
		align-items: baseline;
		justify-content: start;
		gap: 0.625rem;
	}
	.capture-time[data-layout="compact"] .capture-time__date {
		font-size: 0.9375rem;
	}
</style>
