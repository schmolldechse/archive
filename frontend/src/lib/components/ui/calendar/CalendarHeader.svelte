<script lang="ts">
	import type { CalendarHeaderProps } from "./types";

	let { children, ref = $bindable(null), class: className, ...restProps }: CalendarHeaderProps = $props();
</script>

<div {...restProps} bind:this={ref} class={["calendar-header", className]} data-calendar-header>
	{@render children()}
</div>

<style>
	.calendar-header {
		display: grid;
		grid-template-columns: minmax(2.75rem, auto) minmax(0, 1fr) minmax(2.75rem, auto);
		align-items: center;
		gap: 0.5rem;
		border-block-end: 1px solid var(--border-trace);
		padding: 0.5rem;
	}
</style>
