<script lang="ts">
	import { getCalendarContext } from "./calendar-context.svelte";
	import { toCalendarMonthKey } from "./helpers";
	import type { CalendarHeadingProps } from "./types";

	let { children, ref = $bindable(null), class: className, id, ...restProps }: CalendarHeadingProps = $props();

	const calendar = getCalendarContext("CalendarHeading");
	const state = $derived(calendar.getHeadingState());
</script>

<h2
	{...restProps}
	bind:this={ref}
	id={id ?? calendar.headingId}
	class={["calendar-heading", className]}
	aria-live="polite"
	aria-atomic="true"
	data-calendar-heading
	data-month={toCalendarMonthKey(state.visibleMonth)}
>
	{#if children}
		{@render children(state)}
	{:else}
		{state.label}
	{/if}
</h2>

<style>
	.calendar-heading {
		min-width: 0;
		margin: 0;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 1rem;
		font-weight: 600;
		line-height: 1.5;
		text-align: center;
	}
</style>
