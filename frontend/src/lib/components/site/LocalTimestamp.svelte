<script lang="ts">
	import { DateTime } from "luxon";
	import { onMount } from "svelte";
	import type { HTMLAttributes } from "svelte/elements";

	type NativeTimeProps = Omit<HTMLAttributes<HTMLTimeElement>, "children">;

	interface LocalTimestampProps extends NativeTimeProps {
		value: string;
		format?: "date" | "datetime" | "time";
		ref?: HTMLTimeElement | null;
	}

	let { value, format = "datetime", ref = $bindable(null), class: className, ...restProps }: LocalTimestampProps = $props();

	let formatted = $state<string | null>(null);

	onMount(() => {
		const parsed = DateTime.fromISO(value, { setZone: true });
		const local = parsed.isValid ? parsed.toLocal() : null;

		const formatPattern = {
			date: "yyyy—LL—dd",
			datetime: "yyyy—LL—dd · HH:mm:ss ZZ",
			time: "HH:mm:ss ZZ"
		}[format];

		formatted = local?.toFormat(formatPattern) ?? "Invalid timestamp";
	});
</script>

<time
	{...restProps}
	bind:this={ref}
	datetime={value}
	class={["local-timestamp", className]}
	data-component="local-timestamp"
	data-state={formatted ? "ready" : "loading"}
>
	{formatted ?? "····—··—·· · ··:··:·· ·····"}
</time>

<style>
	.local-timestamp {
		font-family: var(--font-record);
		font-variant-numeric: tabular-nums;
	}

	.local-timestamp[data-state="loading"] {
		color: var(--marginal-note);
	}
</style>
