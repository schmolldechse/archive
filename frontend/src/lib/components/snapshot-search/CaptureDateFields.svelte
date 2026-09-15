<script lang="ts">
	import CalendarIcon from "@lucide/svelte/icons/calendar-days";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import X from "@lucide/svelte/icons/x";
	import Button from "$lib/components/ui/Button.svelte";
	import {
		CalendarGrid,
		CalendarHeader,
		CalendarHeading,
		CalendarNextButton,
		CalendarPreviousButton,
		CalendarRoot
	} from "$lib/components/ui/calendar";
	import { DialogClose, DialogContent, DialogRoot, DialogTitle } from "$lib/components/ui/dialog";
	import { DateTime } from "luxon";
	import type { Attachment } from "svelte/attachments";
	import type { HTMLButtonAttributes } from "svelte/elements";
	import { parseDate, type SearchDraft, type SearchErrors } from "./search-state";

	let {
		draft,
		errors,
		onChange
	}: { draft: SearchDraft; errors: SearchErrors; onChange: (patch: Partial<SearchDraft>) => void } = $props();
	const fields = [
		{ key: "from", label: "Captured from", action: "Choose start date" },
		{ key: "through", label: "Captured through", action: "Choose end date" }
	] as const;
	let open = $state(false);
	let restoreFocus = $state(true);
	let endpoint = $state<"from" | "through">("from");
	let month = $state<DateTime | null>(null);
	let today = $state<DateTime | null>(null);
	let trigger = $state<HTMLElement | null>(null);
	let calendar = $state<HTMLDivElement | null>(null);
	let session = $state(0);
	const from = $derived(parseDate(draft.from));
	const through = $derived(parseDate(draft.through));
	const selected = $derived(endpoint === "from" ? from : through);
	const title = $derived(endpoint === "from" ? "Choose start date" : "Choose end date");

	function show(field: "from" | "through", button: HTMLElement): void {
		if (open && endpoint === field) {
			open = false;
			return;
		}
		endpoint = field;
		trigger = button;
		restoreFocus = true;
		today = DateTime.local().startOf("day");
		month = (parseDate(draft[field]) ?? parseDate(draft[field === "from" ? "through" : "from"]) ?? today).startOf("month");
		session += 1;
		open = true;
	}
	function choose(date: DateTime | null): void {
		if (!date) return;
		onChange({ [endpoint]: date.toISODate() ?? "" });
		open = false;
	}
	function calendarClick(field: "from" | "through"): NonNullable<HTMLButtonAttributes["onclick"]> {
		return (event) => show(field, event.currentTarget);
	}
	function confirmSelectedDay(event: MouseEvent | KeyboardEvent): void {
		if (event instanceof KeyboardEvent && event.key !== "Enter" && event.key !== " ") return;
		const day =
			event.target instanceof Element ? event.target.closest<HTMLButtonElement>("[data-calendar-day][data-selected]") : null;
		// Calendar emits changes only; confirming the existing day also closes the picker.
		if (day && day.getAttribute("aria-disabled") !== "true") choose(parseDate(day.dataset.date ?? ""));
	}

	// DOM integration only: keep the overlapping dialog inside the viewport horizontally.
	// Vertical placement stays beneath its trigger in document coordinates while scrolling.
	const anchorDialog: Attachment<HTMLDialogElement> = (dialog) => {
		if (!open || !trigger) return;
		const anchor = trigger;
		const update = () => {
			if (!dialog.open || !anchor.isConnected) return;
			const width = document.documentElement.clientWidth;
			dialog.style.maxWidth = `${width}px`;
			const bounds = anchor.getBoundingClientRect();
			const anchorLeft = bounds.left + window.scrollX;
			const left = Math.max(0, Math.min(anchorLeft, width - dialog.offsetWidth));
			dialog.style.left = `${left - anchorLeft}px`;
		};
		const observer = new ResizeObserver(update);
		observer.observe(anchor);
		observer.observe(dialog);
		window.addEventListener("resize", update);
		window.addEventListener("scroll", update, true);
		queueMicrotask(() => {
			update();
			if (dialog.open && dialog.getBoundingClientRect().bottom > window.innerHeight - 8) {
				if (dialog.offsetHeight + anchor.offsetHeight + 8 > window.innerHeight) {
					anchor.scrollIntoView({ block: "start", behavior: "instant" });
				} else {
					dialog.scrollIntoView({ block: "end", behavior: "instant" });
				}
			}
		});
		return () => {
			observer.disconnect();
			window.removeEventListener("resize", update);
			window.removeEventListener("scroll", update, true);
		};
	};
</script>

{#each fields as field (field.key)}
	<div class="date-field" data-component="capture-date-field" data-state={errors[field.key] ? "invalid" : "default"}>
		<label for={`capture-${field.key}-control`}>{field.label}</label>
		<div class="date-anchor">
			<Button
				id={`capture-${field.key}-control`}
				class="date-control"
				aria-haspopup="dialog"
				aria-controls={`capture-${field.key}-picker`}
				aria-expanded={open && endpoint === field.key}
				aria-invalid={errors[field.key] ? "true" : undefined}
				aria-describedby={errors[field.key] ? `capture-${field.key}-error` : undefined}
				aria-label={`${field.action}${draft[field.key] ? `, ${draft[field.key]}` : ", no date selected"}`}
				onpointerdown={(event: PointerEvent) => {
					// Let the trigger toggle its own picker instead of first dismissing it as an outside click.
					if (open && endpoint === field.key) event.stopPropagation();
				}}
				onclick={calendarClick(field.key)}
			>
				<span class:date-value={Boolean(draft[field.key])} class:date-placeholder={!draft[field.key]}>
					{draft[field.key] || "Choose date"}
				</span>
				<CalendarIcon aria-hidden="true" />
			</Button>
			{#if endpoint === field.key && month}
				<DialogRoot
					{open}
					modal={false}
					onOpenChange={(value, reason) => {
						restoreFocus = reason !== "outside";
						open = value;
					}}
				>
					<DialogContent
						id={`capture-${field.key}-picker`}
						size="small"
						class="capture-date-dialog"
						closeOnOutsidePointer
						preventScroll={false}
						{restoreFocus}
						returnFocus={() => trigger}
						initialFocus={() => calendar?.querySelector<HTMLButtonElement>('[data-calendar-day][tabindex="0"]') ?? null}
						{@attach anchorDialog}
					>
						<div class="dialog-heading">
							<DialogTitle>{title}</DialogTitle>
							<DialogClose aria-label="Cancel date selection"><X aria-hidden="true" /></DialogClose>
						</div>
						{#key session}
							<CalendarRoot
								type="single"
								value={selected}
								visibleMonth={month}
								{today}
								bind:ref={calendar}
								minValue={endpoint === "through" ? (from ?? undefined) : undefined}
								maxValue={endpoint === "from" ? (through ?? undefined) : undefined}
								onVisibleMonthChange={(value) => {
									month = value;
								}}
								onValueChange={choose}
								class="capture-date-calendar"
							>
								<CalendarHeader><CalendarPreviousButton /><CalendarHeading /><CalendarNextButton /></CalendarHeader>
								<CalendarGrid weekdayFormat="narrow" onclick={confirmSelectedDay} onkeydown={confirmSelectedDay} />
							</CalendarRoot>
						{/key}
						<Button
							variant="text"
							class="clear-date"
							onclick={() => {
								onChange({ [endpoint]: "" });
								open = false;
							}}
						>
							Clear date
						</Button>
					</DialogContent>
				</DialogRoot>
			{/if}
		</div>
		{#if errors[field.key]}
			<p class="date-error" id={`capture-${field.key}-error`} role="alert">
				<CircleAlert aria-hidden="true" />{errors[field.key]}
			</p>
		{/if}
	</div>
{/each}

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
	.date-anchor {
		position: relative;
		min-width: 0;
	}
	.date-anchor :global(.date-control) {
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
	.date-anchor :global(.date-control[aria-invalid="true"]) {
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
		font-size: 0.875rem;
		color: var(--time-marker);
	}
	.date-error :global(svg) {
		width: 1rem;
		height: 1.25rem;
		flex: none;
	}
	.date-anchor :global([data-dialog-content].capture-date-dialog[data-modal="false"]) {
		position: absolute;
		inset: calc(100% + 0.5rem) auto auto 0;
		z-index: 50;
		margin: 0;
		width: 24rem;
		max-width: 100dvw;
		max-height: none;
		border: 1px solid var(--border-trace);
		padding: 1rem;
		overflow: visible;
		transform: none;
	}
	.dialog-heading {
		display: flex;
		align-items: start;
		justify-content: space-between;
		gap: 0.5rem;
	}
	.dialog-heading :global([data-dialog-title]) {
		font-size: 1.75rem;
	}
	.dialog-heading :global(button) {
		flex: none;
		min-width: 44px;
		padding-inline: 0.5rem;
	}
	:global(.capture-date-dialog [data-calendar-root].capture-date-calendar) {
		display: grid;
		width: 100%;
		min-width: 308px;
		justify-self: stretch;
		margin-top: 1rem;
	}
	:global(.capture-date-dialog .capture-date-calendar [data-calendar-grid]) {
		width: 100%;
		min-width: 308px;
	}
	:global(.capture-date-dialog .capture-date-calendar [data-calendar-day]) {
		min-width: 44px;
		min-height: 44px;
		padding-inline: 2px;
	}
	:global(.capture-date-dialog .capture-date-calendar [data-calendar-grid] th) {
		padding-inline: 0;
	}
	:global(.capture-date-dialog .capture-date-calendar [data-calendar-header]) {
		grid-template-columns: 44px minmax(0, 1fr) 44px;
		padding-inline: 2px;
	}
	:global(.capture-date-dialog .capture-date-calendar [data-calendar-next]),
	:global(.capture-date-dialog .capture-date-calendar [data-calendar-previous]) {
		width: 44px;
		min-height: 44px;
	}
	:global(.capture-date-dialog .clear-date) {
		margin-top: 0.5rem;
	}
	@media (max-width: 26rem) {
		.date-anchor :global([data-dialog-content].capture-date-dialog[data-modal="false"]) {
			padding-inline: 0;
		}
		.dialog-heading {
			padding-inline: 0.5rem;
		}
		:global(.capture-date-dialog [data-calendar-root].capture-date-calendar) {
			border-inline-width: 0;
		}
	}
</style>
