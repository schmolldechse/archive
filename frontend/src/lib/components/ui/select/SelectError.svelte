<script lang="ts">
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import { onDestroy } from "svelte";

	import { getSelectContext } from "./select-context.svelte";
	import type { SelectErrorProps } from "./types";

	let {
		children,
		ref = $bindable(null),
		class: className,
		id,
		...restProps
	}: SelectErrorProps = $props();

	const select = getSelectContext("SelectError");
	const fallbackId = select.defaultErrorId;
	const unregisterId = select.registerErrorId(() => id ?? fallbackId);

	onDestroy(unregisterId);
</script>

<p
	{...restProps}
	bind:this={ref}
	id={id ?? fallbackId}
	hidden={!select.invalid}
	class={["select-error", className]}
	data-select-error
	data-visible={select.invalid ? "" : undefined}
>
	<span class="select-error__icon" aria-hidden="true"><CircleAlert size={18} strokeWidth={1.75} /></span>
	<span class="select-error__message">{@render children()}</span>
</p>

<style>
	.select-error {
		display: flex;
		align-items: flex-start;
		gap: 0.5rem;
		margin: 0;
		border-inline-start: 2px solid var(--time-marker);
		padding-inline-start: 0.75rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 0.875rem;
		line-height: 1.43;
	}

	.select-error__icon {
		display: inline-flex;
		width: 1.125rem;
		height: 1.125rem;
		flex: none;
		align-items: center;
		justify-content: center;
		margin-block-start: 0.0625rem;
		color: var(--time-marker);
	}

	.select-error__message {
		min-width: 0;
		overflow-wrap: anywhere;
	}
</style>
