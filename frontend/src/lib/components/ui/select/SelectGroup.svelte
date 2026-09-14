<script lang="ts">
	import { setSelectGroupContext } from "./select-context.svelte";
	import type { SelectGroupProps } from "./types";

	const generatedId = $props.id();

	let {
		children,
		ref = $bindable(null),
		class: className,
		id = generatedId,
		"aria-label": ariaLabel,
		"aria-labelledby": ariaLabelledby,
		...restProps
	}: SelectGroupProps = $props();

	let labelIdGetters = $state<Array<() => string>>([]);
	const defaultLabelId = $derived(`${id}-label`);

	setSelectGroupContext({
		get defaultLabelId() {
			return defaultLabelId;
		},
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
	{id}
	role="group"
	class={["select-group", className]}
	aria-label={ariaLabel}
	aria-labelledby={ariaLabelledby ?? (ariaLabel ? undefined : labelIdGetters.at(-1)?.())}
	data-select-group
>
	{@render children()}
</div>

<style>
	.select-group {
		display: grid;
	}
</style>
