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

	let mounted = $state(false);
	const parsed = $derived(DateTime.fromISO(value, { setZone: true }));
	const local = $derived(mounted ? parsed.toLocal() : parsed);
	const formatted = $derived(
		local.isValid
			? local.toFormat(
					{
						date: "yyyy-LL-dd",
						datetime: "yyyy-LL-dd · HH:mm:ss",
						time: "HH:mm:ss"
					}[format]
				)
			: "Invalid timestamp"
	);

	onMount(() => {
		mounted = true;
	});
</script>

<time
	{...restProps}
	bind:this={ref}
	datetime={value}
	class={["local-timestamp", className]}
	data-component="local-timestamp"
	data-state={local.isValid ? "ready" : "invalid"}
>
	{formatted}
</time>

<style>
	.local-timestamp {
		font-family: var(--font-record);
		font-variant-numeric: tabular-nums;
	}
</style>
