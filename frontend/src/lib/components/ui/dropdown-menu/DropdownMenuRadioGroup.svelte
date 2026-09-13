<script lang="ts">
	import { setDropdownMenuRadioGroupContext } from "./dropdown-menu-context.svelte";
	import type { DropdownMenuRadioGroupProps } from "./types";

	let {
		value = $bindable(""),
		onValueChange,
		children
	}: DropdownMenuRadioGroupProps = $props();

	const registeredValueGetters: Array<() => string> = [];

	const assertUniqueValues = () => {
		const values = registeredValueGetters.map((getter) => getter());
		if (new Set(values).size === values.length) return;

		const duplicate = values.find((entry, index) => values.indexOf(entry) !== index);
		throw new Error(`DropdownMenuRadioItem values must be unique. Duplicate value: "${duplicate}".`);
	};

	setDropdownMenuRadioGroupContext({
		get value() {
			return value;
		},
		commit(nextValue) {
			assertUniqueValues();
			if (value === nextValue) return;
			value = nextValue;
			onValueChange?.(nextValue);
		},
		registerValue(getValue) {
			registeredValueGetters.push(getValue);
			assertUniqueValues();
			return () => {
				const index = registeredValueGetters.indexOf(getValue);
				if (index !== -1) registeredValueGetters.splice(index, 1);
			};
		}
	});
</script>

{@render children()}
