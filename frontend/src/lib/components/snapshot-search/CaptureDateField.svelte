<script lang="ts">
	import CalendarIcon from "@lucide/svelte/icons/calendar-days";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import { DialogRoot, DialogTrigger } from "$lib/components/ui/dialog";
	import { DateTime } from "luxon";
	import CaptureDateDialog from "./CaptureDateDialog.svelte";
	import { parseDate } from "./search-state";

	let {
		id,
		label,
		description,
		action,
		value,
		error,
		minValue,
		maxValue,
		onChange
	}: {
		id: string;
		label: string;
		description?: string;
		action: string;
		value: string;
		error?: string;
		minValue?: string;
		maxValue?: string;
		onChange: (value: string) => void;
	} = $props();

	let open = $state(false);
	let month = $state<DateTime | null>(null);
	let today = $state<DateTime | null>(null);
	const selected = $derived(parseDate(value));
	const minimum = $derived(parseDate(minValue ?? "") ?? undefined);
	const maximum = $derived(parseDate(maxValue ?? "") ?? undefined);
	const controlId = $derived(`${id}-control`);
	const dialogId = $derived(`${id}-dialog`);
	const errorId = $derived(`${id}-error`);
	const descriptionId = $derived(`${id}-description`);

	function prepareDialog(): void {
		today = DateTime.local().startOf("day");
		month = (selected ?? minimum ?? maximum ?? today).startOf("month");
	}

	function handleOpenChange(nextOpen: boolean): void {
		if (nextOpen) prepareDialog();
		open = nextOpen;
	}

	function choose(date: DateTime): void {
		onChange(date.toISODate() ?? "");
		open = false;
	}
</script>

<div class="date-field" data-component="capture-date-field" data-state={error ? "invalid" : "default"}>
	<label for={controlId}>{label} <span class="field-qualifier">Optional</span></label>
	<DialogRoot {open} modal={false} onOpenChange={handleOpenChange}>
		<DialogTrigger
			id={controlId}
			class="date-control"
			aria-invalid={error ? "true" : undefined}
			aria-describedby={[description ? descriptionId : null, error ? errorId : null].filter(Boolean).join(" ") || undefined}
			aria-label={`${action}${value ? `, ${value}` : ", no date selected"}`}
		>
			<span class:date-value={Boolean(value)} class:date-placeholder={!value}>{value || "Choose date"}</span>
			<CalendarIcon aria-hidden="true" />
		</DialogTrigger>
		{#if month && today}
			<CaptureDateDialog
				id={dialogId}
				title={action}
				{selected}
				visibleMonth={month}
				{today}
				minValue={minimum}
				maxValue={maximum}
				position="trigger"
				onVisibleMonthChange={(value) => (month = value)}
				onSelect={choose}
			/>
		{/if}
	</DialogRoot>
	{#if description}<p class="date-description" id={descriptionId}>{description}</p>{/if}
	{#if error}
		<p class="date-error" id={errorId} role="alert">
			<CircleAlert aria-hidden="true" />{error}
		</p>
	{/if}
</div>

<style>
	.date-field {
		display: grid;
		align-content: start;
		min-width: 0;
		gap: 0.5rem;
	}

	label {
		font-size: 0.9375rem;
		font-weight: 600;
		line-height: 1.4;
	}

	:global([data-dialog-trigger].date-control[data-variant="secondary"]) {
		width: 100%;
		min-height: 3.5rem;
		justify-content: space-between;
		background: var(--reading-room);
		padding: 0.875rem 1rem;
		font-size: 1rem;
		font-weight: 400;
		line-height: 1.5;
		text-align: left;
	}

	:global([data-dialog-trigger].date-control[aria-invalid="true"]) {
		border: 2px solid var(--time-marker);
		padding: calc(0.875rem - 1px) calc(1rem - 1px);
	}

	.date-value {
		font-family: var(--font-record);
		font-variant-numeric: tabular-nums;
		overflow-wrap: anywhere;
	}

	.date-placeholder {
		color: var(--marginal-note);
	}

	.date-error {
		display: flex;
		align-items: start;
		gap: 0.5rem;
		margin: 0;
		border-inline-start: 2px solid var(--time-marker);
		padding-inline-start: 0.75rem;
		font-size: 0.875rem;
		line-height: 1.43;
		color: var(--reading-ink);
	}
	.date-description {
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.43;
	}

	.date-error :global(svg) {
		width: 1rem;
		height: 1.25rem;
		flex: none;
		color: var(--time-marker);
	}
</style>
