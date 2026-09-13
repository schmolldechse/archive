<script lang="ts">
	import CircleAlert from "@lucide/svelte/icons/circle-alert";

	import { getInputContext } from "./input-context.svelte";
	import type { InputErrorProps } from "./types";

	let { children, ref = $bindable(null), class: className, ...restProps }: InputErrorProps = $props();

	const input = getInputContext("InputError");
</script>

{#if input.invalid}
	<p {...restProps} bind:this={ref} id={input.errorId} class={["input-error", className]} data-input-error>
		<span class="input-error__icon" aria-hidden="true">
			<CircleAlert size={18} strokeWidth={1.75} />
		</span>
		<span class="input-error__message">{@render children()}</span>
	</p>
{/if}

<style>
	.input-error {
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

	.input-error__icon {
		display: inline-flex;
		width: 1.125rem;
		height: 1.125rem;
		flex: none;
		align-items: center;
		justify-content: center;
		margin-block-start: 0.0625rem;
		color: var(--time-marker);
	}

	.input-error__message {
		min-width: 0;
		overflow-wrap: anywhere;
	}
</style>
