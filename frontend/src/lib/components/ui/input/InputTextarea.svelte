<script lang="ts">
	import { getInputContext } from "./input-context.svelte";
	import type { InputTextareaProps } from "./types";

	let {
		value = $bindable(),
		ref = $bindable(null),
		class: className,
		"aria-describedby": consumerDescribedBy,
		...restProps
	}: InputTextareaProps = $props();
	const input = getInputContext("InputTextarea");
	const describedBy = $derived(
		[
			input.hasDescription ? input.descriptionId : undefined,
			input.invalid ? input.errorId : undefined,
			...(consumerDescribedBy?.split(/\s+/) ?? [])
		]
			.filter(Boolean)
			.join(" ") || undefined
	);
</script>

<textarea
	{...restProps}
	bind:this={ref}
	bind:value
	id={input.controlId}
	disabled={input.disabled}
	required={input.required}
	class={["input-textarea", className]}
	aria-invalid={input.invalid ? "true" : undefined}
	aria-describedby={describedBy}
	data-input-control
	data-state={input.state}></textarea>

<style>
	.input-textarea {
		box-sizing: border-box;
		width: 100%;
		min-width: 0;
		min-height: 7.25rem;
		resize: vertical;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		color: var(--reading-ink);
		padding: 0.875rem 1rem;
		font: 400 1rem/1.5 var(--font-interface);
	}
	.input-textarea::placeholder {
		color: var(--marginal-note);
		opacity: 1;
	}
	.input-textarea:hover:not(:disabled):not(:focus-visible) {
		border-color: var(--reading-ink);
	}
	.input-textarea[aria-invalid="true"] {
		border: 2px solid var(--time-marker);
		padding: calc(0.875rem - 1px) calc(1rem - 1px);
	}
	.input-textarea:disabled {
		cursor: not-allowed;
		opacity: 0.62;
	}
</style>
