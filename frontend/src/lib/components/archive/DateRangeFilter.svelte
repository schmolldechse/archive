<script lang="ts">
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import { DateTime } from "luxon";

	import Button from "$lib/components/ui/Button.svelte";
	import { InputControl, InputLabel, InputRoot } from "$lib/components/ui/input";

	export interface CaptureRange {
		from?: string;
		to?: string;
	}

	interface DateRangeFilterProps {
		from?: string;
		to?: string;
		onApply: (range: CaptureRange) => void;
		onClear: () => void;
	}

	let { from, to, onApply, onClear }: DateRangeFilterProps = $props();
	let error = $state<string | null>(null);

	const fromDate = $derived(toInputDate(from));
	const throughDate = $derived(toInputDate(to, true));

	function toInputDate(value: string | undefined, exclusiveEnd = false): string {
		if (!value) return "";

		const parsed = DateTime.fromISO(value, { setZone: true });
		if (!parsed.isValid) return "";

		return (exclusiveEnd ? parsed.minus({ days: 1 }) : parsed).toISODate() ?? "";
	}

	function serializeBoundary(value: string, exclusiveEnd = false): string | undefined {
		if (!value) return undefined;

		const parsed = DateTime.fromISO(value, { zone: "local" }).startOf("day");
		const boundary = exclusiveEnd ? parsed.plus({ days: 1 }).startOf("day") : parsed;
		return boundary.isValid ? (boundary.toISO({ suppressMilliseconds: true }) ?? undefined) : undefined;
	}

	function applyRange(event: SubmitEvent): void {
		event.preventDefault();
		const form = event.currentTarget as HTMLFormElement;
		const formData = new FormData(form);
		const selectedFrom = String(formData.get("fromDate") ?? "");
		const selectedThrough = String(formData.get("throughDate") ?? "");

		if (selectedFrom && selectedThrough && selectedFrom > selectedThrough) {
			error = "The through date must be the same as or later than the from date.";
			return;
		}

		const serializedFrom = serializeBoundary(selectedFrom);
		const serializedTo = serializeBoundary(selectedThrough, true);

		if ((selectedFrom && !serializedFrom) || (selectedThrough && !serializedTo)) {
			error = "Enter a valid capture range.";
			return;
		}

		error = null;
		onApply({ from: serializedFrom, to: serializedTo });
	}

	function clearRange(): void {
		error = null;
		onClear();
	}
</script>

<form class="date-range" aria-labelledby="date-range-title" onsubmit={applyRange}>
	<div class="date-range__heading">
		<p id="date-range-title">Capture range</p>
		<span>Local dates</span>
	</div>

	<div class="date-range__fields">
		<InputRoot id="capture-from">
			<InputLabel>From date</InputLabel>
			<InputControl
				type="date"
				name="fromDate"
				value={fromDate}
				max={throughDate || undefined}
				data-value-kind="record"
				aria-describedby={error ? "capture-range-error" : undefined}
			/>
		</InputRoot>

		<InputRoot id="capture-through">
			<InputLabel>Through date</InputLabel>
			<InputControl
				type="date"
				name="throughDate"
				value={throughDate}
				min={fromDate || undefined}
				data-value-kind="record"
				aria-describedby={error ? "capture-range-error" : undefined}
			/>
		</InputRoot>
	</div>

	{#if error}
		<p class="date-range__error" id="capture-range-error" role="alert">
			<CircleAlert aria-hidden="true" />
			<span>{error}</span>
		</p>
	{/if}

	<p class="date-range__note">Both dates are included. Boundaries use your current local offset.</p>

	<div class="date-range__actions">
		<Button variant="primary" type="submit">Apply range</Button>
		<Button variant="text" type="button" onclick={clearRange} disabled={!from && !to}>Clear dates</Button>
	</div>
</form>

<style>
	.date-range {
		position: sticky;
		top: 1.5rem;
		display: grid;
		gap: 1.25rem;
		padding-top: 1.5rem;
	}

	.date-range__heading {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 0.75rem;
		border-bottom: 1px solid var(--border-trace);
		padding-bottom: 0.75rem;
	}

	.date-range__heading p,
	.date-range__heading span {
		margin: 0;
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}

	.date-range__heading span {
		color: var(--marginal-note);
		font-family: var(--font-record);
		font-size: 0.6875rem;
		letter-spacing: 0.02em;
		text-transform: none;
	}

	.date-range__fields {
		display: grid;
		gap: 1rem;
	}

	.date-range :global([data-input-control]) {
		background: var(--reading-room);
	}

	.date-range__note,
	.date-range__error {
		margin: 0;
		font-size: 0.8125rem;
		line-height: 1.43;
	}

	.date-range__note {
		color: var(--marginal-note);
	}

	.date-range__error {
		display: flex;
		align-items: flex-start;
		gap: 0.5rem;
		border-inline-start: 2px solid var(--time-marker);
		padding-inline-start: 0.75rem;
	}

	.date-range__error :global(svg) {
		width: 1.125rem;
		height: 1.125rem;
		flex: none;
		margin-top: 0.0625rem;
		color: var(--time-marker);
		stroke-width: 1.75;
	}

	.date-range__actions {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem;
	}

	@media (max-width: 48rem) {
		.date-range {
			position: static;
			grid-template-columns: minmax(0, 1fr) auto;
			align-items: end;
			border-bottom: 1px solid var(--border-trace);
			padding-bottom: 1.25rem;
		}

		.date-range__heading,
		.date-range__error,
		.date-range__note {
			grid-column: 1 / -1;
		}

		.date-range__fields {
			grid-template-columns: repeat(2, minmax(0, 1fr));
		}
	}

	@media (max-width: 34rem) {
		.date-range {
			grid-template-columns: 1fr;
		}

		.date-range__fields {
			grid-template-columns: 1fr;
		}

		.date-range__actions {
			grid-column: 1;
		}
	}
</style>
