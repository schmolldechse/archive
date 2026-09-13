<script lang="ts">
	import type { Attachment } from "svelte/attachments";

	import { mergeClasses, stringArraysEqual } from "./helpers";
	import { setToggleGroupContext, type ToggleGroupItemRegistration } from "./toggle-group-context.svelte";
	import type { MultipleToggleGroupProps, SingleToggleGroupProps, ToggleGroupRootProps } from "./types";

	type ToggleGroupValue = string | null | string[];

	let {
		type,
		value = $bindable(type === "single" ? null : []),
		onValueChange,
		selectionRequired = false,
		orientation = "horizontal",
		loop = true,
		rovingFocus = true,
		semanticRole = "group",
		disabled = false,
		children,
		ref = $bindable(null),
		class: className,
		"aria-label": ariaLabel,
		"aria-labelledby": ariaLabelledby,
		...restProps
	}: ToggleGroupRootProps = $props();

	let focusedValue = $state<string | null>(null);
	let itemRegistrations = $state.raw<ToggleGroupItemRegistration[]>([]);
	let orderRevision = $state(0);

	const accessibleName = $derived.by(() => {
		if (!ariaLabel?.trim() && !ariaLabelledby?.trim()) {
			throw new Error("ToggleGroupRoot requires an aria-label or aria-labelledby value.");
		}

		return {
			label: ariaLabel?.trim() || undefined,
			labelledby: ariaLabelledby?.trim() || undefined
		};
	});

	const assertUniqueValues = (): void => {
		const values = new Set<string>();

		for (const item of itemRegistrations) {
			if (values.has(item.value)) {
				throw new Error(`ToggleGroupItem values must be unique. Duplicate value: ${JSON.stringify(item.value)}.`);
			}

			values.add(item.value);
		}
	};

	const getOrderedItems = (): ToggleGroupItemRegistration[] => {
		void orderRevision;
		assertUniqueValues();
		const registrationOrder = new Map(itemRegistrations.map((registration, index) => [registration, index]));

		return [...itemRegistrations].sort((left, right) => {
			if (left.element?.isConnected && right.element?.isConnected) {
				const position = left.element.compareDocumentPosition(right.element);

				if (position & 4) return -1;
				if (position & 2) return 1;
			}

			return (registrationOrder.get(left) ?? 0) - (registrationOrder.get(right) ?? 0);
		});
	};

	const getRegisteredItem = (targetValue: string): ToggleGroupItemRegistration | undefined => {
		assertUniqueValues();
		return itemRegistrations.find((registration) => registration.value === targetValue);
	};

	const isItemDisabled = (targetValue: string, itemDisabled: boolean): boolean => {
		return disabled || itemDisabled || getRegisteredItem(targetValue)?.disabled === true;
	};

	const getEnabledItems = (): ToggleGroupItemRegistration[] => {
		return getOrderedItems().filter((registration) => !disabled && !registration.disabled);
	};

	const isPressed = (targetValue: string): boolean => {
		assertUniqueValues();
		return type === "single" ? value === targetValue : Array.isArray(value) && value.includes(targetValue);
	};

	const orderValues = (values: Iterable<string>): string[] => {
		const remaining = new Set(values);
		const ordered = getOrderedItems().flatMap((item) => {
			if (!remaining.delete(item.value)) return [];
			return [item.value];
		});

		return [...ordered, ...remaining];
	};

	const commit = (nextValue: ToggleGroupValue): void => {
		if (type === "single") {
			const nextSingleValue = nextValue as string | null;
			if (value === nextSingleValue) return;

			value = nextSingleValue;
			(onValueChange as SingleToggleGroupProps["onValueChange"] | undefined)?.(nextSingleValue);
			return;
		}

		const nextMultipleValue = nextValue as string[];
		const currentValue = Array.isArray(value) ? value : [];
		if (stringArraysEqual(currentValue, nextMultipleValue)) return;

		value = nextMultipleValue;
		(onValueChange as MultipleToggleGroupProps["onValueChange"] | undefined)?.(nextMultipleValue);
	};

	const toggle = (targetValue: string, itemDisabled: boolean): void => {
		if (isItemDisabled(targetValue, itemDisabled)) return;

		if (type === "single") {
			if (value === targetValue) {
				if (!selectionRequired) commit(null);
				return;
			}

			commit(targetValue);
			return;
		}

		const currentValue = Array.isArray(value) ? value : [];
		const nextValues = new Set(currentValue);

		if (nextValues.has(targetValue)) {
			if (selectionRequired && nextValues.size === 1) return;
			nextValues.delete(targetValue);
		} else {
			nextValues.add(targetValue);
		}

		commit(orderValues(nextValues));
	};

	const getTabStopValue = (): string | undefined => {
		const enabledItems = getEnabledItems();

		return (
			enabledItems.find((item) => isPressed(item.value))?.value ??
			enabledItems.find((item) => item.value === focusedValue)?.value ??
			enabledItems[0]?.value
		);
	};

	const getTabIndex = (targetValue: string, itemDisabled: boolean): number | undefined => {
		if (isItemDisabled(targetValue, itemDisabled)) return -1;
		if (!rovingFocus) return undefined;

		return getTabStopValue() === targetValue ? 0 : -1;
	};

	const focusItem = (targetValue: string, itemDisabled: boolean): void => {
		if (!isItemDisabled(targetValue, itemDisabled)) focusedValue = targetValue;
	};

	const navigate = (event: KeyboardEvent, currentValue: string): void => {
		if (!rovingFocus || event.altKey || event.ctrlKey || event.metaKey) return;

		const enabledItems = getEnabledItems();
		if (enabledItems.length === 0) return;

		const currentIndex = enabledItems.findIndex((item) => item.value === currentValue);
		let nextIndex: number;

		switch (event.key) {
			case "ArrowLeft":
				if (orientation !== "horizontal") return;
				nextIndex = currentIndex === -1 ? enabledItems.length - 1 : currentIndex - 1;
				break;
			case "ArrowRight":
				if (orientation !== "horizontal") return;
				nextIndex = currentIndex === -1 ? 0 : currentIndex + 1;
				break;
			case "ArrowUp":
				if (orientation !== "vertical") return;
				nextIndex = currentIndex === -1 ? enabledItems.length - 1 : currentIndex - 1;
				break;
			case "ArrowDown":
				if (orientation !== "vertical") return;
				nextIndex = currentIndex === -1 ? 0 : currentIndex + 1;
				break;
			case "Home":
				nextIndex = 0;
				break;
			case "End":
				nextIndex = enabledItems.length - 1;
				break;
			default:
				return;
		}

		event.preventDefault();

		if (nextIndex < 0 || nextIndex >= enabledItems.length) {
			if (!loop) return;
			nextIndex = (nextIndex + enabledItems.length) % enabledItems.length;
		}

		enabledItems[nextIndex]?.element?.focus();
	};

	const registerItem = (item: ToggleGroupItemRegistration): (() => void) => {
		itemRegistrations = [...itemRegistrations, item];
		assertUniqueValues();

		return () => {
			itemRegistrations = itemRegistrations.filter((registration) => registration !== item);
		};
	};

	const observeItemOrder: Attachment<HTMLDivElement> = (element) => {
		const observer = new MutationObserver(() => {
			orderRevision += 1;
		});

		observer.observe(element, { childList: true, subtree: true });

		return () => observer.disconnect();
	};

	setToggleGroupContext({
		get disabled() {
			return disabled;
		},
		get loop() {
			return loop;
		},
		get orientation() {
			return orientation;
		},
		get rovingFocus() {
			return rovingFocus;
		},
		focusItem,
		getTabIndex,
		isItemDisabled,
		isPressed,
		navigate,
		registerItem,
		toggle
	});
</script>

<div
	{...restProps}
	bind:this={ref}
	class={mergeClasses("toggle-group-root", className)}
	role={semanticRole}
	aria-label={accessibleName.label}
	aria-labelledby={accessibleName.labelledby}
	aria-orientation={semanticRole === "toolbar" && orientation === "vertical" ? "vertical" : undefined}
	data-component="toggle-group"
	data-type={type}
	data-orientation={orientation}
	data-disabled={disabled ? "" : undefined}
	{@attach observeItemOrder}
>
	{@render children()}
</div>

<style>
	.toggle-group-root {
		display: inline-flex;
		max-width: 100%;
		flex-wrap: nowrap;
		align-items: stretch;
		overflow-x: auto;
		padding: 0.375rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
	}

	.toggle-group-root[data-orientation="vertical"] {
		width: fit-content;
		flex-direction: column;
		flex-wrap: nowrap;
		overflow-x: visible;
	}

	.toggle-group-root[data-orientation="horizontal"] :global([data-toggle-group-item]:not(:first-child)) {
		margin-inline-start: -1px;
	}

	.toggle-group-root[data-orientation="horizontal"] :global([data-toggle-group-item]:first-child) {
		border-radius: var(--radius-control) 0 0 var(--radius-control);
	}

	.toggle-group-root[data-orientation="horizontal"] :global([data-toggle-group-item]:last-child) {
		border-radius: 0 var(--radius-control) var(--radius-control) 0;
	}

	.toggle-group-root[data-orientation="vertical"] :global([data-toggle-group-item]) {
		width: 100%;
		justify-content: flex-start;
	}

	.toggle-group-root[data-orientation="vertical"] :global([data-toggle-group-item]:not(:first-child)) {
		margin-block-start: -1px;
	}

	.toggle-group-root[data-orientation="vertical"] :global([data-toggle-group-item]:first-child) {
		border-radius: var(--radius-control) var(--radius-control) 0 0;
	}

	.toggle-group-root[data-orientation="vertical"] :global([data-toggle-group-item]:last-child) {
		border-radius: 0 0 var(--radius-control) var(--radius-control);
	}

	.toggle-group-root :global([data-toggle-group-item]:only-child) {
		border-radius: var(--radius-control);
	}

	.toggle-group-root[data-orientation="vertical"] :global([data-toggle-group-item][data-state="on"]) {
		box-shadow: inset 3px 0 0 var(--register-mark);
	}
</style>
