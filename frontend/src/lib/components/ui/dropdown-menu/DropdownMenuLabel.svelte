<script lang="ts">
	import { onDestroy } from "svelte";

	import { getDropdownMenuGroupContext } from "./dropdown-menu-context.svelte";
	import type { DropdownMenuLabelProps } from "./types";

	const generatedId = $props.id();

	let {
		children,
		ref = $bindable(null),
		class: className,
		id,
		...restProps
	}: DropdownMenuLabelProps = $props();

	const group = getDropdownMenuGroupContext("DropdownMenuLabel");
	const unregisterId = group.registerLabelId(() => id ?? generatedId);
	onDestroy(unregisterId);
</script>

<div
	{...restProps}
	bind:this={ref}
	id={id ?? generatedId}
	class={["dropdown-menu-label", className]}
	data-dropdown-menu-label
>
	{@render children()}
</div>

<style>
	.dropdown-menu-label {
		padding: 0.625rem 0.75rem 0.375rem;
		color: var(--marginal-note);
		font-family: var(--font-interface);
		font-size: 0.75rem;
		font-weight: 600;
		line-height: 1.33;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
</style>
