<script lang="ts">
	import { getToastItemContext } from "./toast-context.svelte";
	import type { ToastActionProps } from "./types";

	let {
		children,
		ref = $bindable(null),
		class: className,
		disabled = false,
		onclick,
		"aria-busy": ariaBusy,
		...restProps
	}: ToastActionProps = $props();

	const toast = getToastItemContext("ToastAction");
	const action = $derived.by(() => {
		if (!toast.record.action) throw new Error("ToastAction requires the current toast to define an action.");
		return toast.record.action;
	});
	let pending = $state(false);

	const handleClick: NonNullable<ToastActionProps["onclick"]> = async (event) => {
		onclick?.(event);
		if (event.defaultPrevented || disabled || pending) return;

		const currentAction = action;
		pending = true;
		try {
			await currentAction.onAction();
			if (currentAction.dismissOnAction ?? true) toast.dismiss();
		} finally {
			pending = false;
		}
	};
</script>

<button
	{...restProps}
	bind:this={ref}
	type="button"
	class={["toast-action", className]}
	disabled={disabled || pending}
	aria-busy={pending ? "true" : ariaBusy}
	data-toast-action
	data-pending={pending ? "" : undefined}
	onclick={handleClick}
>
	{#if children}{@render children()}{:else}{action.label}{/if}
</button>

<style>
	.toast-action {
		display: inline-flex;
		min-height: 2.75rem;
		align-items: center;
		justify-content: center;
		box-sizing: border-box;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: transparent;
		padding: 0.6875rem 0.875rem;
		color: var(--register-mark);
		font-size: 0.875rem;
		font-weight: 600;
		line-height: 1.25rem;
		text-decoration-line: underline;
		text-decoration-thickness: 0.08em;
		text-underline-offset: 0.16em;
		cursor: pointer;
		transition:
			background-color 150ms ease,
			border-color 150ms ease,
			opacity 150ms ease;
	}

	.toast-action:hover:not(:disabled) {
		border-color: var(--reading-ink);
		background: var(--reading-room);
	}

	.toast-action:disabled {
		cursor: wait;
		opacity: 0.58;
	}

	@media (prefers-reduced-motion: reduce) {
		.toast-action {
			transition: none;
		}
	}
</style>
