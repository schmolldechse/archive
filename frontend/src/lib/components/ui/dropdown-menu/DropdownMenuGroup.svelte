<script lang="ts">
	import { setDropdownMenuGroupContext } from "./dropdown-menu-context.svelte";
	import type { DropdownMenuGroupProps } from "./types";

	let {
		children,
		ref = $bindable(null),
		class: className,
		"aria-label": ariaLabel,
		"aria-labelledby": ariaLabelledby,
		...restProps
	}: DropdownMenuGroupProps = $props();

	let labelIdGetters = $state<Array<() => string>>([]);

	setDropdownMenuGroupContext({
		get labelId() {
			return labelIdGetters.at(-1)?.();
		},
		registerLabelId(getId) {
			labelIdGetters = [...labelIdGetters, getId];
			return () => (labelIdGetters = labelIdGetters.filter((entry) => entry !== getId));
		}
	});
</script>

<div
	{...restProps}
	bind:this={ref}
	role="group"
	class={["dropdown-menu-group", className]}
	aria-label={ariaLabel}
	aria-labelledby={ariaLabelledby ?? (ariaLabel ? undefined : labelIdGetters.at(-1)?.())}
	data-dropdown-menu-group
>
	{@render children()}
</div>

<style>
	.dropdown-menu-group {
		display: grid;
	}
</style>
