<script lang="ts">
	import { onMount } from "svelte";
	import { DateTime } from "luxon";
	import type { HTMLAttributes } from "svelte/elements";

	type NativeTimeProps = Omit<HTMLAttributes<HTMLTimeElement>, "children">;

	interface ArchiveTimestampProps extends NativeTimeProps {
		value: string;
		density?: "default" | "compact";
		ref?: HTMLTimeElement | null;
	}

	let { value, density = "default", ref = $bindable(null), class: className, ...restProps }: ArchiveTimestampProps = $props();

	let mounted = $state(false);
	onMount(() => {
		mounted = true;
	});

	const sourceDate = $derived(DateTime.fromISO(value, { setZone: true }).setLocale("en"));
	// Resolve the viewer's timezone only after hydration so server and browser markup match.
	const localDate = $derived(mounted ? sourceDate.toLocal() : sourceDate);
</script>

<time
	{...restProps}
	bind:this={ref}
	datetime={value}
	class={["archive-timestamp", className]}
	data-component="archive-timestamp"
	data-state={localDate.isValid ? (mounted ? "local" : "resolving") : "invalid"}
	data-density={density}
	aria-label={localDate.isValid ? localDate.toFormat("MMMM d, yyyy 'at' HH:mm") : "Invalid timestamp"}
>
	{#if localDate.isValid}
		<span class="archive-timestamp__date" aria-hidden="true">{localDate.toFormat("d LLL yyyy")}</span>
		<span class="archive-timestamp__time" aria-hidden="true">{localDate.toFormat("HH:mm")}</span>
	{:else}
		<span>Invalid timestamp</span>
	{/if}
</time>

<style>
	.archive-timestamp {
		display: inline-flex;
		align-items: baseline;
		gap: 0.5rem;
		color: var(--reading-ink);
		font-family: var(--font-record);
		font-size: 0.8125rem;
		line-height: 1.4;
		font-variant-numeric: tabular-nums;
		white-space: nowrap;
		vertical-align: middle;
	}
	.archive-timestamp__date {
		color: var(--marginal-note);
	}
	.archive-timestamp__time {
		padding-inline-start: 0.5rem;
		border-inline-start: 1px solid var(--border-trace);
		font-weight: 500;
	}
	.archive-timestamp[data-density="compact"] {
		gap: 0.375rem;
		font-size: 0.75rem;
	}
	.archive-timestamp[data-density="compact"] .archive-timestamp__time {
		padding-inline-start: 0.375rem;
	}
</style>
