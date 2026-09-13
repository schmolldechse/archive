<script lang="ts">
	import { getInputContext } from "./input-context.svelte";
	import type { InputControlProps } from "./types";

	let {
		type = "text",
		value = $bindable(),
		ref = $bindable(null),
		class: className,
		"aria-describedby": consumerDescribedBy,
		"data-value-kind": valueKind = "text",
		...restProps
	}: InputControlProps = $props();

	const input = getInputContext("InputControl");
	const describedBy = $derived.by(() => {
		const ids = [
			input.hasDescription ? input.descriptionId : undefined,
			input.invalid ? input.errorId : undefined,
			...(consumerDescribedBy?.split(/\s+/) ?? [])
		].filter((candidate): candidate is string => Boolean(candidate));

		return [...new Set(ids)].join(" ") || undefined;
	});
</script>

<input
	{...restProps}
	bind:this={ref}
	bind:value
	id={input.controlId}
	{type}
	disabled={input.disabled}
	required={input.required}
	class={["input-control", className]}
	aria-invalid={input.invalid ? "true" : undefined}
	aria-describedby={describedBy}
	data-input-control
	data-state={input.state}
	data-value-kind={valueKind}
/>

<style>
	.input-control {
		box-sizing: border-box;
		width: 100%;
		min-width: 0;
		min-height: 3.5rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		padding: 0.875rem 1rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 1rem;
		line-height: 1.5;
		transition:
			border-color 150ms ease,
			background-color 150ms ease;
	}

	.input-control::placeholder {
		color: var(--marginal-note);
		opacity: 1;
	}

	.input-control:hover:not(:disabled):not(:focus-visible) {
		border-color: var(--reading-ink);
	}

	.input-control[aria-invalid="true"] {
		border-color: var(--time-marker);
		border-width: 2px;
		padding: calc(0.875rem - 1px) calc(1rem - 1px);
	}

	.input-control:disabled {
		cursor: not-allowed;
		opacity: 0.62;
	}

	.input-control:read-only:not(:disabled) {
		background: color-mix(in srgb, var(--archive-layer) 72%, var(--reading-room));
	}

	.input-control[data-value-kind="record"] {
		font-family: var(--font-record);
		font-variant-numeric: tabular-nums;
	}

	@media (prefers-reduced-motion: reduce) {
		.input-control {
			transition: none;
		}
	}
</style>
