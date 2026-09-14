<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import { CalendarRoot as CalendarRootMeta } from "$lib/components/ui/calendar";

	const { Story } = defineMeta({
		title: "Components/Calendar",
		component: CalendarRootMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A composable Gregorian calendar grid for deterministic single-date or contiguous-range selection, localized labels, constrained availability, and complete keyboard navigation."
				}
			}
		}
	});
</script>

<script lang="ts">
	import { DateTime } from "luxon";

	import {
		CalendarGrid,
		CalendarHeader,
		CalendarHeading,
		CalendarNextButton,
		CalendarPreviousButton,
		CalendarRoot,
		type CalendarChangeSource,
		type CalendarRange
	} from "$lib/components/ui/calendar";

	let singleValue = $state<DateTime | null>(DateTime.utc(2024, 3, 14));
	let singleMonth = $state(DateTime.utc(2024, 3, 1));
	let singleSource = $state<CalendarChangeSource | null>(null);

	let rangeValue = $state<CalendarRange | null>({ start: DateTime.utc(2024, 3, 12), end: null });
	let rangeMonth = $state(DateTime.utc(2024, 3, 1));

	let constrainedValue = $state<DateTime | null>(DateTime.utc(2024, 3, 14));
	let constrainedMonth = $state(DateTime.utc(2024, 3, 1));
	const unavailableDates = new Set(["2024-03-13", "2024-03-18", "2024-03-19"]);

	let readonlyValue = $state<DateTime | null>(DateTime.utc(2024, 3, 14));
	let readonlyMonth = $state(DateTime.utc(2024, 3, 1));

	let boundValue = $state<CalendarRange | null>({
		start: DateTime.utc(2024, 4, 8),
		end: DateTime.utc(2024, 4, 12)
	});
	let boundMonth = $state(DateTime.utc(2024, 4, 1));
	let boundValueSource = $state<CalendarChangeSource | null>(null);
	let boundMonthCallback = $state<DateTime | null>(null);

	let englishValue = $state<DateTime | null>(null);
	let englishMonth = $state(DateTime.utc(2024, 3, 1));
	let germanValue = $state<DateTime | null>(null);
	let germanMonth = $state(DateTime.utc(2024, 3, 1));

	let fixedValue = $state<DateTime | null>(null);
	let fixedMonth = $state(DateTime.utc(2024, 2, 1));
	let variableValue = $state<DateTime | null>(null);
	let variableMonth = $state(DateTime.utc(2024, 2, 1));

	let metadataValue = $state<DateTime | null>(DateTime.utc(2024, 3, 5));
	let metadataMonth = $state(DateTime.utc(2024, 3, 1));
	const metadataDates = new Set(["2024-03-05", "2024-03-11", "2024-03-22"]);

	let boundaryMonth = $state(DateTime.utc(2023, 12, 1));
	let boundaryValue = $state<DateTime | null>(DateTime.utc(2023, 12, 31));
	let leapMonth = $state(DateTime.utc(2024, 2, 1));
	let leapValue = $state<DateTime | null>(DateTime.utc(2024, 2, 29));

	let keyboardMonth = $state(DateTime.utc(2024, 3, 1));
	let keyboardValue = $state<DateTime | null>(DateTime.utc(2024, 3, 14));

	const formatDate = (value: DateTime | null): string => value?.toISODate() ?? "null";
	const formatMonth = (value: DateTime | null): string => value?.toFormat("yyyy-MM") ?? "none";
	const formatRange = (value: CalendarRange | null): string =>
		value === null ? "null" : `${formatDate(value.start)} → ${value.end === null ? "…" : formatDate(value.end)}`;
</script>

{#snippet CalendarControls()}
	<CalendarHeader>
		<CalendarPreviousButton />
		<CalendarHeading />
		<CalendarNextButton />
	</CalendarHeader>
{/snippet}

<Story name="Single Date" asChild>
	<section class="story-section">
		<p class="story-label">Single selection</p>
		<CalendarRoot
			type="single"
			bind:value={singleValue}
			bind:visibleMonth={singleMonth}
			today={DateTime.utc(2024, 3, 20)}
			onValueChange={(_, source) => (singleSource = source)}
		>
			{@render CalendarControls()}
			<CalendarGrid />
		</CalendarRoot>
		<p class="story-register" aria-live="polite">
			<span>Value</span>
			<code>{formatDate(singleValue)}</code>
			<span>Visible month</span>
			<code>{formatMonth(singleMonth)}</code>
			<span>Last source</span>
			<code>{singleSource ?? "none"}</code>
		</p>
	</section>
</Story>

<Story name="Range Selection" asChild>
	<section class="story-section">
		<p class="story-label">Incomplete range and preview</p>
		<p class="story-note">
			Hover or move keyboard focus from the committed start to preview a contiguous range. Activate a second date to complete
			it; the next activation starts again.
		</p>
		<CalendarRoot type="range" bind:value={rangeValue} bind:visibleMonth={rangeMonth} today={DateTime.utc(2024, 3, 20)}>
			{@render CalendarControls()}
			<CalendarGrid />
		</CalendarRoot>
		<p class="story-register" aria-live="polite">
			<span>Range</span>
			<code>{formatRange(rangeValue)}</code>
		</p>
	</section>
</Story>

<Story name="Availability Constraints" asChild>
	<section class="story-section">
		<p class="story-label">Minimum, maximum, and unavailable dates</p>
		<p class="story-note">
			The selectable interval is 8–24 March. Callback-unavailable dates keep their grid position and keyboard focus but cannot
			be selected.
		</p>
		<CalendarRoot
			type="single"
			bind:value={constrainedValue}
			bind:visibleMonth={constrainedMonth}
			today={DateTime.utc(2024, 3, 20)}
			minValue={DateTime.utc(2024, 3, 8)}
			maxValue={DateTime.utc(2024, 3, 24)}
			isDateUnavailable={(date) => unavailableDates.has(date.toISODate() ?? "")}
		>
			{@render CalendarControls()}
			<CalendarGrid />
		</CalendarRoot>
	</section>
</Story>

<Story name="Disabled and Read Only" asChild>
	<div class="story-grid">
		<section class="story-section">
			<p class="story-label">Disabled</p>
			<CalendarRoot
				type="single"
				value={DateTime.utc(2024, 3, 14)}
				visibleMonth={DateTime.utc(2024, 3, 1)}
				today={DateTime.utc(2024, 3, 20)}
				disabled
			>
				{@render CalendarControls()}
				<CalendarGrid />
			</CalendarRoot>
		</section>

		<section class="story-section">
			<p class="story-label">Read only</p>
			<p class="story-note">Navigation and focus remain available while activation leaves the value unchanged.</p>
			<CalendarRoot
				type="single"
				bind:value={readonlyValue}
				bind:visibleMonth={readonlyMonth}
				today={DateTime.utc(2024, 3, 20)}
				readonly
			>
				{@render CalendarControls()}
				<CalendarGrid />
			</CalendarRoot>
			<p class="story-register"><span>Value</span><code>{formatDate(readonlyValue)}</code></p>
		</section>
	</div>
</Story>

<Story name="Bound Values" asChild>
	<section class="story-section">
		<p class="story-label">Luxon DateTime bindings</p>
		<CalendarRoot
			type="range"
			bind:value={boundValue}
			bind:visibleMonth={boundMonth}
			today={DateTime.utc(2024, 4, 16)}
			onValueChange={(_, source) => (boundValueSource = source)}
			onVisibleMonthChange={(month) => (boundMonthCallback = month)}
		>
			{@render CalendarControls()}
			<CalendarGrid />
		</CalendarRoot>
		<div class="story-register" aria-live="polite">
			<span>Value</span>
			<code>{formatRange(boundValue)}</code>
			<span>Visible month</span>
			<code>{formatMonth(boundMonth)}</code>
			<span>Value source</span>
			<code>{boundValueSource ?? "none"}</code>
			<span>Month callback</span>
			<code>{formatMonth(boundMonthCallback)}</code>
		</div>
	</section>
</Story>

<Story name="Locales and Week Starts" asChild>
	<div class="story-grid">
		<section class="story-section">
			<p class="story-label">English · Sunday first</p>
			<CalendarRoot
				type="single"
				bind:value={englishValue}
				bind:visibleMonth={englishMonth}
				today={DateTime.utc(2024, 3, 20)}
				locale="en-US"
				weekStartsOn={0}
			>
				{@render CalendarControls()}
				<CalendarGrid weekdayFormat="narrow" />
			</CalendarRoot>
		</section>

		<section class="story-section">
			<p class="story-label">Deutsch · Montag zuerst</p>
			<CalendarRoot
				type="single"
				bind:value={germanValue}
				bind:visibleMonth={germanMonth}
				today={DateTime.utc(2024, 3, 20)}
				locale="de-DE"
				weekStartsOn={1}
			>
				{@render CalendarControls()}
				<CalendarGrid weekdayFormat="short" />
			</CalendarRoot>
		</section>
	</div>
</Story>

<Story name="Grid Geometry" asChild>
	<div class="story-grid">
		<section class="story-section">
			<p class="story-label">Fixed six weeks · outside dates visible</p>
			<CalendarRoot
				type="single"
				bind:value={fixedValue}
				bind:visibleMonth={fixedMonth}
				today={DateTime.utc(2024, 2, 14)}
				fixedWeeks
				showOutsideDays
			>
				{@render CalendarControls()}
				<CalendarGrid />
			</CalendarRoot>
		</section>

		<section class="story-section">
			<p class="story-label">Variable weeks · outside dates hidden</p>
			<CalendarRoot
				type="single"
				bind:value={variableValue}
				bind:visibleMonth={variableMonth}
				today={DateTime.utc(2024, 2, 14)}
				fixedWeeks={false}
				showOutsideDays={false}
			>
				{@render CalendarControls()}
				<CalendarGrid />
			</CalendarRoot>
		</section>
	</div>
</Story>

<Story name="Custom Day Content" asChild>
	<section class="story-section">
		<p class="story-label">Non-interactive metadata markers</p>
		<p class="story-note">The marker supplements the retained numeric day and does not add an interactive descendant.</p>
		<CalendarRoot type="single" bind:value={metadataValue} bind:visibleMonth={metadataMonth} today={DateTime.utc(2024, 3, 20)}>
			{@render CalendarControls()}
			<CalendarGrid>
				{#snippet day(state)}
					<span>{state.dayNumber}</span>
					{#if metadataDates.has(state.date.toISODate() ?? "")}
						<span class="metadata-marker" aria-hidden="true"></span>
					{/if}
				{/snippet}
			</CalendarGrid>
		</CalendarRoot>
	</section>
</Story>

<Story name="Month and Year Boundaries" asChild>
	<div class="story-grid">
		<section class="story-section">
			<p class="story-label">Year boundary</p>
			<p class="story-note">Use Next month or Arrow Right from 31 December to cross into January.</p>
			<CalendarRoot
				type="single"
				bind:value={boundaryValue}
				bind:visibleMonth={boundaryMonth}
				today={DateTime.utc(2023, 12, 20)}
			>
				{@render CalendarControls()}
				<CalendarGrid />
			</CalendarRoot>
		</section>

		<section class="story-section">
			<p class="story-label">Leap-year February</p>
			<p class="story-note">Page Down preserves the day number when possible and clamps it when the target month is shorter.</p>
			<CalendarRoot type="single" bind:value={leapValue} bind:visibleMonth={leapMonth} today={DateTime.utc(2024, 2, 14)}>
				{@render CalendarControls()}
				<CalendarGrid />
			</CalendarRoot>
		</section>
	</div>
</Story>

<Story name="Keyboard and Mobile Width" asChild>
	<div class="story-keyboard-layout">
		<section class="story-section story-mobile">
			<p class="story-label">Constrained presentation</p>
			<CalendarRoot
				type="single"
				bind:value={keyboardValue}
				bind:visibleMonth={keyboardMonth}
				today={DateTime.utc(2024, 3, 20)}
			>
				{@render CalendarControls()}
				<CalendarGrid />
			</CalendarRoot>
		</section>

		<section class="keyboard-reference" aria-labelledby="keyboard-reference-heading">
			<h3 id="keyboard-reference-heading">Keyboard reference</h3>
			<dl>
				<div>
					<dt>Arrow keys</dt>
					<dd>Move by one day or one week</dd>
				</div>
				<div>
					<dt>Home / End</dt>
					<dd>Move to the first or last day of the week</dd>
				</div>
				<div>
					<dt>Page Up / Down</dt>
					<dd>Move by one month</dd>
				</div>
				<div>
					<dt>Shift + Page Up / Down</dt>
					<dd>Move by one year</dd>
				</div>
				<div>
					<dt>Enter / Space</dt>
					<dd>Select the focused date</dd>
				</div>
			</dl>
		</section>
	</div>
</Story>

<style>
	.story-section {
		display: grid;
		align-content: start;
		justify-items: start;
		gap: 0.75rem;
		min-width: 0;
	}

	.story-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(min(100%, 21rem), 1fr));
		gap: 3rem;
		max-width: 64rem;
	}

	.story-label,
	.story-note,
	.story-register,
	.keyboard-reference {
		font-family: var(--font-interface);
	}

	.story-label {
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		line-height: 1.33;
		text-transform: uppercase;
	}

	.story-note {
		max-width: 36rem;
		margin: 0;
		border-inline-start: 2px solid var(--register-mark);
		padding-inline-start: 0.75rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.43;
	}

	.story-register {
		display: flex;
		max-width: 42rem;
		flex-wrap: wrap;
		gap: 0.5rem 1rem;
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.43;
	}

	.story-register code {
		color: var(--reading-ink);
		font-family: var(--font-record);
		font-variant-numeric: tabular-nums;
		overflow-wrap: anywhere;
	}

	.metadata-marker {
		position: absolute;
		inset-block-end: 0.3rem;
		inset-inline-end: 0.35rem;
		width: 0.3rem;
		height: 0.3rem;
		border: 1px solid currentColor;
		border-radius: var(--radius-mark);
		background: var(--reading-room);
	}

	.story-keyboard-layout {
		display: grid;
		grid-template-columns: minmax(0, auto) minmax(16rem, 24rem);
		align-items: start;
		gap: 3rem;
	}

	.story-mobile {
		width: 20rem;
		max-width: 100%;
		overflow-x: auto;
		padding: 0.25rem;
	}

	.keyboard-reference {
		border-block: 1px solid var(--border-trace);
		padding-block: 1rem;
	}

	.keyboard-reference h3 {
		margin: 0 0 0.75rem;
		font-size: 1rem;
		font-weight: 600;
	}

	.keyboard-reference dl,
	.keyboard-reference dl div {
		display: grid;
	}

	.keyboard-reference dl {
		gap: 0;
		margin: 0;
	}

	.keyboard-reference dl div {
		grid-template-columns: minmax(8rem, 0.8fr) minmax(0, 1.2fr);
		gap: 1rem;
		border-block-start: 1px solid color-mix(in srgb, var(--border-trace) 52%, transparent);
		padding-block: 0.5rem;
	}

	.keyboard-reference dt {
		font-family: var(--font-record);
		font-size: 0.8125rem;
		font-weight: 500;
	}

	.keyboard-reference dd {
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}

	@media (max-width: 44rem) {
		.story-keyboard-layout {
			grid-template-columns: 1fr;
		}
	}
</style>
