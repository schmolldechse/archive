<script lang="ts">
	import { DateTime } from "luxon";
	import type { HTMLAttributes } from "svelte/elements";

	type NativeTimeProps = Omit<HTMLAttributes<HTMLTimeElement>, "children">;

	interface ArchiveTimestampProps extends NativeTimeProps {
		value: string;
		ref?: HTMLTimeElement | null;
	}

	let { value, ref = $bindable(null), class: className, ...restProps }: ArchiveTimestampProps = $props();

	/** Formats each archive timestamp in UTC so server rendering and every viewer agree. */
	const parsed = $derived(DateTime.fromISO(value, { setZone: true }).toUTC());
	const formatted = $derived(parsed.isValid ? parsed.toFormat("yyyy-LL-dd · HH:mm:ss 'UTC'") : "Invalid timestamp");
</script>

<time
	{...restProps}
	bind:this={ref}
	datetime={value}
	class={["archive-timestamp", className]}
	data-component="archive-timestamp"
	data-state={parsed.isValid ? "ready" : "invalid"}
>
	{formatted}
</time>

<style>
	.archive-timestamp {
		font-family: var(--font-record);
		font-variant-numeric: tabular-nums;
	}
</style>
