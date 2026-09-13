<script lang="ts">
	import { setAccordionRootContext, type AccordionRootContext } from "./accordion-context.svelte";
	import { mergeClasses } from "./helpers";
	import type { AccordionRootProps, MultipleAccordionProps, SingleAccordionProps } from "./types";

	type AccordionValue = string | null | string[];
	type ItemRegistration = Parameters<AccordionRootContext["registerItem"]>[0];

	let {
		type,
		value = $bindable(type === "single" ? null : []),
		onValueChange,
		collapsible = true,
		disabled = false,
		headingLevel = 3,
		keyboardNavigation = true,
		loop = true,
		children,
		ref = $bindable(null),
		class: className,
		...restProps
	}: AccordionRootProps = $props();

	const items: ItemRegistration[] = [];

	const assertUniqueValues = () => {
		const values = new Set<string>();

		for (const item of items) {
			if (values.has(item.value)) {
				throw new Error(`AccordionItem values must be unique. Duplicate value: "${item.value}".`);
			}

			values.add(item.value);
		}
	};

	const registerItem = (item: ItemRegistration) => {
		items.push(item);
		assertUniqueValues();

		return () => {
			const index = items.indexOf(item);

			if (index !== -1) items.splice(index, 1);
		};
	};

	const isOpen = (itemValue: string): boolean => {
		assertUniqueValues();

		return type === "single" ? value === itemValue : Array.isArray(value) && value.includes(itemValue);
	};

	const isCollapseDisabled = (itemValue: string): boolean => {
		return type === "single" && !collapsible && isOpen(itemValue);
	};

	const orderedValues = (values: Iterable<string>): string[] => {
		assertUniqueValues();
		const remaining = new Set(values);
		const ordered = items.flatMap((item) => {
			if (!remaining.delete(item.value)) return [];
			return [item.value];
		});

		return [...ordered, ...remaining];
	};

	const commit = (nextValue: AccordionValue) => {
		if (type === "single") {
			const nextSingleValue = nextValue as string | null;

			if (value === nextSingleValue) return;

			value = nextSingleValue;
			(onValueChange as SingleAccordionProps["onValueChange"] | undefined)?.(nextSingleValue);
			return;
		}

		const nextMultipleValue = nextValue as string[];
		const currentValue = Array.isArray(value) ? value : [];

		if (
			currentValue.length === nextMultipleValue.length &&
			currentValue.every((entry, index) => entry === nextMultipleValue[index])
		) {
			return;
		}

		value = nextMultipleValue;
		(onValueChange as MultipleAccordionProps["onValueChange"] | undefined)?.(nextMultipleValue);
	};

	const toggle = (itemValue: string, itemDisabled: boolean) => {
		if (disabled || itemDisabled) return;

		if (type === "single") {
			if (value === itemValue) {
				if (collapsible) commit(null);
				return;
			}

			commit(itemValue);
			return;
		}

		const currentValue = Array.isArray(value) ? value : [];
		const nextValues = new Set(currentValue);

		if (nextValues.has(itemValue)) nextValues.delete(itemValue);
		else nextValues.add(itemValue);

		commit(orderedValues(nextValues));
	};

	const openFromSearch = (itemValue: string) => {
		if (isOpen(itemValue)) return;

		if (type === "single") {
			commit(itemValue);
			return;
		}

		const currentValue = Array.isArray(value) ? value : [];
		commit(orderedValues([...currentValue, itemValue]));
	};

	const navigate = (event: KeyboardEvent) => {
		if (!keyboardNavigation) return;

		const current = event.currentTarget;
		if (!(current instanceof HTMLButtonElement)) return;

		const root = current.closest('[data-component="accordion"]');
		if (!(root instanceof HTMLElement)) return;

		const triggers = Array.from(root.querySelectorAll<HTMLButtonElement>("[data-accordion-trigger]")).filter(
			(trigger) => trigger.closest('[data-component="accordion"]') === root && !trigger.disabled
		);
		const currentIndex = triggers.indexOf(current);
		if (currentIndex === -1 || triggers.length === 0) return;

		let nextIndex: number | null = null;

		switch (event.key) {
			case "ArrowDown":
			case "ArrowRight":
				nextIndex = currentIndex + 1;
				break;
			case "ArrowUp":
			case "ArrowLeft":
				nextIndex = currentIndex - 1;
				break;
			case "Home":
				nextIndex = 0;
				break;
			case "End":
				nextIndex = triggers.length - 1;
				break;
			default:
				return;
		}

		if (nextIndex < 0 || nextIndex >= triggers.length) {
			if (!loop) return;
			nextIndex = (nextIndex + triggers.length) % triggers.length;
		}

		event.preventDefault();
		triggers[nextIndex]?.focus();
	};

	setAccordionRootContext({
		get disabled() {
			return disabled;
		},
		get headingLevel() {
			return headingLevel;
		},
		get keyboardNavigation() {
			return keyboardNavigation;
		},
		get loop() {
			return loop;
		},
		isCollapseDisabled,
		isOpen,
		navigate,
		openFromSearch,
		registerItem,
		toggle
	});
</script>

<div
	{...restProps}
	bind:this={ref}
	class={mergeClasses("accordion-root", className)}
	data-component="accordion"
	data-type={type}
	data-disabled={disabled ? "" : undefined}
>
	{@render children()}
</div>

<style>
	.accordion-root {
		width: 100%;
		border-block-start: 1px solid var(--border-trace);
		color: var(--reading-ink);
		font-family: var(--font-interface);
	}
</style>
