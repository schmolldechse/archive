<script lang="ts">
	import type { CaptureDistributionResponse } from "$api";
	import { DateTime } from "luxon";
	import { captureSegments, draftFromState, parseDate, type SearchState } from "./search-state";
	let {
		distribution,
		committed,
		pending = false,
		onPeriod
	}: {
		distribution: CaptureDistributionResponse | null;
		committed: SearchState;
		pending?: boolean;
		onPeriod: (from: string, through: string) => void;
	} = $props();
	const segments = $derived(captureSegments(distribution));
	const dates = $derived(draftFromState(committed));
	const selected = $derived(segments.find((segment) => segment.from === dates.from && segment.through === dates.through));
	const active = $derived(Boolean(dates.from || dates.through));
	const caption = $derived(
		active
			? `${selected ? "Capture period" : "Custom period"} · ${dates.from ? `From ${dates.from}` : ""}${dates.from && dates.through ? " · " : ""}${dates.through ? `Through ${dates.through}` : ""}`
			: "All indexed years"
	);
	const band = $derived.by(() => {
		if (!active || !segments.length) return null;
		const start = DateTime.local(segments[0].firstYear, 1, 1).toMillis();
		const end = DateTime.local(segments[segments.length - 1].lastYear + 1, 1, 1).toMillis();
		const from = parseDate(dates.from)?.toMillis() ?? start;
		const through = parseDate(dates.through)?.plus({ days: 1 }).toMillis() ?? end;
		const left = Math.max(start, from),
			right = Math.min(end, through);
		return right > left
			? { left: 4 + (92 * (left - start)) / (end - start), width: (92 * (right - left)) / (end - start) }
			: null;
	});
</script>

<div class="time-register" data-component="capture-timeline" aria-label="Filter results by capture period" aria-busy={pending}>
	{#if segments.length}
		<div class="timeline-canvas">
			<div class="topline">
				<span><strong>Capture density</strong> · select a period</span><span class:active>{caption}</span>
			</div>
			<div class="register-line" aria-hidden="true"></div>
			{#if band}<div class="period-band" aria-hidden="true" style={`left:${band.left}%;width:${band.width}%`}></div>{/if}
			{#each segments as segment (segment.firstYear)}
				<span
					class="year-label"
					style={`left:${4 + (92 * (segment.firstYear - segments[0].firstYear)) / (segments[segments.length - 1].lastYear - segments[0].firstYear + 1)}%`}
					>{segment.firstYear}</span
				>
				{#if segment.count > 0}
					<button
						class="time-window"
						type="button"
						disabled={pending}
						aria-pressed={selected?.firstYear === segment.firstYear}
						aria-label={`${selected?.firstYear === segment.firstYear ? "Remove period" : "Filter to"} ${segment.label}, ${segment.count} snapshots`}
						style={`left:${segment.position}%;--point-size:${segment.size}px`}
						onclick={() => onPeriod(segment.from, segment.through)}
					>
						<span class="count">{String(segment.count).padStart(2, "0")}</span><span class="point" aria-hidden="true"
						></span><span class="period-label">{segment.label}</span>
					</button>
				{:else}
					<span class="empty-window" style={`left:${segment.position}%`}
						><span>00</span><span class="empty-point" aria-hidden="true"></span><span>{segment.label}</span></span
					>
				{/if}
			{/each}
			<span class="year-label end-year">{segments[segments.length - 1].lastYear}</span>
		</div>
	{:else}
		<div class="empty-timeline">
			<strong>Capture density</strong>
			<div class="empty-line" aria-hidden="true"></div>
			<p>No capture periods available</p>
		</div>
	{/if}
</div>

<style>
	.time-register {
		border-block: 1px solid var(--border-trace);
		background: color-mix(in srgb, var(--archive-layer) 45%, transparent);
		overflow-x: auto;
	}
	.timeline-canvas {
		position: relative;
		min-width: 40rem;
		min-height: 12rem;
		padding: 1rem 1.5rem;
	}
	.topline {
		display: flex;
		justify-content: space-between;
		flex-wrap: wrap;
		gap: 0.5rem 1.5rem;
		font-size: 0.75rem;
		color: var(--marginal-note);
	}
	.topline strong {
		color: var(--reading-ink);
		font-weight: 600;
	}
	.topline .active {
		color: var(--time-marker);
	}
	.register-line {
		position: absolute;
		top: 7rem;
		left: 4%;
		right: 4%;
		height: 1px;
		background: var(--border-trace);
	}
	.period-band {
		position: absolute;
		top: 6.75rem;
		height: 0.5rem;
		border-block: 2px solid var(--time-marker);
		background: color-mix(in srgb, var(--time-marker) 20%, transparent);
	}
	.year-label {
		position: absolute;
		top: 3.75rem;
		font-family: var(--font-record);
		font-size: 0.75rem;
		transform: translateX(-50%);
		color: var(--marginal-note);
	}
	.end-year {
		left: 96%;
	}
	.time-window,
	.empty-window {
		position: absolute;
		top: 5rem;
		display: grid;
		grid-template-rows: 1.5rem 1rem auto;
		justify-items: center;
		gap: 0.5rem;
		min-width: 44px;
		min-height: 44px;
		transform: translateX(-50%);
		font-family: var(--font-record);
		font-size: 0.75rem;
	}
	.time-window {
		border: 0;
		background: transparent;
		color: var(--register-mark);
		cursor: pointer;
		padding: 0.25rem;
	}
	.point {
		width: var(--point-size);
		height: var(--point-size);
		align-self: center;
		border: 1px solid currentColor;
		background: currentColor;
		border-radius: 50%;
	}
	.time-window:hover .period-label {
		text-decoration: underline;
	}
	.time-window[aria-pressed="true"] {
		color: var(--time-marker);
		font-weight: 600;
	}
	.time-window[aria-pressed="true"]::before {
		position: absolute;
		left: calc(50% - 1.5px);
		top: 1.75rem;
		width: 3px;
		height: 2rem;
		background: var(--time-marker);
		content: "";
	}
	.time-window[aria-pressed="true"] .point {
		border-radius: 2px;
		outline: 2px solid var(--reading-room);
		z-index: 1;
	}
	.time-window:disabled {
		cursor: wait;
	}
	.empty-window {
		padding-top: 0.25rem;
		color: var(--marginal-note);
	}
	.empty-point {
		width: 8px;
		height: 8px;
		border: 1px solid var(--border-trace);
		border-radius: 50%;
		align-self: center;
	}
	.empty-timeline {
		padding: 1.5rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
	.empty-line {
		height: 1px;
		background: var(--border-trace);
		margin-block: 1.5rem;
	}
	.empty-timeline p {
		margin: 0;
	}
</style>
