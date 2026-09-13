<script lang="ts">
	import { setDropdownMenuCheckboxGroupContext } from "./dropdown-menu-context.svelte";
	import { stringArraysEqual } from "./helpers";
	import type { DropdownMenuCheckboxGroupProps } from "./types";

	let {
		value = $bindable([]),
		onValueChange,
		children
	}: DropdownMenuCheckboxGroupProps = $props();

	const registeredValueGetters: Array<() => string> = [];

	const assertUniqueValues = () => {
		const values = registeredValueGetters.map((getter) => getter());
		if (new Set(values).size === values.length) return;

		const duplicate = values.find((entry, index) => values.indexOf(entry) !== index);
		throw new Error(`DropdownMenuCheckboxItem values must be unique. Duplicate value: "${duplicate}".`);
	};

	setDropdownMenuCheckboxGroupContext({
		get value() {
			return value;
		},
		commit(nextValue) {
			assertUniqueValues();
			const requestedValues = new Set(nextValue);
			const ordered = registeredValueGetters.map((getter) => getter()).filter((entry) => requestedValues.delete(entry));
			ordered.push(...requestedValues);
			if (stringArraysEqual(value, ordered)) return;

			value = ordered;
			onValueChange?.(ordered);
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
