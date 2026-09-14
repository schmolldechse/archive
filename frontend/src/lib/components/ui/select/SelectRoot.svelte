<script lang="ts">
	import { onDestroy } from "svelte";

	import {
		setSelectContext,
		type RegisteredSelectItem,
		type SelectHighlightIntent
	} from "./select-context.svelte";
	import { normalizeSelectText, stringArraysEqual } from "./helpers";
	import type {
		SelectChangeSource,
		SelectItemValue,
		SelectMultipleProps,
		SelectRootProps,
		SelectSingleProps,
		SelectValueState
	} from "./types";

	const generatedId = $props.id();

	let {
		type,
		value = $bindable(type === "single" ? null : []),
		onValueChange,
		open = $bindable(false),
		onOpenChange,
		allowDeselect = false,
		disabled = false,
		required = false,
		invalid = false,
		name,
		loop = false,
		typeahead = true,
		closeOnSelect,
		children,
		ref = $bindable(null),
		class: className,
		id = generatedId,
		...restProps
	}: SelectRootProps = $props();

	const defaultTriggerId = $derived(`${id}-trigger`);
	const defaultListboxId = $derived(`${id}-listbox`);
	const defaultLabelId = $derived(`${id}-label`);
	const defaultDescriptionId = $derived(`${id}-description`);
	const defaultErrorId = $derived(`${id}-error`);
	let triggerIdGetters = $state<Array<() => string>>([]);
	let listboxIdGetters = $state<Array<() => string>>([]);
	let labelIdGetters = $state<Array<() => string>>([]);
	let descriptionIdGetters = $state<Array<() => string>>([]);
	let errorIdGetters = $state<Array<() => string>>([]);
	let items = $state.raw<RegisteredSelectItem[]>([]);
	const triggerGetters: Array<() => HTMLButtonElement | null> = [];
	const viewportGetters: Array<() => HTMLDivElement | null> = [];
	let lastTrigger: HTMLButtonElement | null = null;
	let highlightedId = $state<string | null>(null);
	let restoreFocus = false;
	let reportedOpen = open;
	let typeaheadBuffer = "";
	let typeaheadTimer: ReturnType<typeof setTimeout> | undefined;

	const shouldCloseOnSelect = $derived(closeOnSelect ?? type === "single");

	const sortItems = (entries: RegisteredSelectItem[]): RegisteredSelectItem[] =>
		[...entries].sort((left, right) => {
			const leftElement = left.getElement();
			const rightElement = right.getElement();
			if (!leftElement || !rightElement || leftElement === rightElement || typeof Node === "undefined") return 0;

			const position = leftElement.compareDocumentPosition(rightElement);
			return position & Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1;
		});

	const orderedItems = $derived.by(() => sortItems(items));
	const selectedItems = $derived.by(() => {
		if (type === "single") {
			const selectedValue = typeof value === "string" ? value : null;
			return orderedItems.filter((item) => item.value === selectedValue);
		}

		const selectedValues = new Set(Array.isArray(value) ? value : []);
		return orderedItems.filter((item) => selectedValues.has(item.value));
	});
	const selectedLabels = $derived(
		selectedItems.map((item) => item.getTextValue().trim()).filter((label) => label.length > 0)
	);
	const currentValue: SelectItemValue | null | SelectItemValue[] = $derived.by(() =>
		type === "single"
			? typeof value === "string"
				? value
				: null
			: selectedItems.map((item) => item.value)
	);
	const empty = $derived(type === "single" ? currentValue === null : (currentValue as SelectItemValue[]).length === 0);

	const registerGetter = <T,>(getters: Array<() => T>, getter: () => T) => {
		getters.push(getter);
		return () => {
			const index = getters.indexOf(getter);
			if (index !== -1) getters.splice(index, 1);
		};

	};

	const registerId = (
		getGetters: () => Array<() => string>,
		getId: () => string,
		assign: (getters: Array<() => string>) => void
	) => {
		assign([...getGetters(), getId]);
		return () => assign(getGetters().filter((entry) => entry !== getId));
	};

	const enabledItems = () => orderedItems.filter((item) => !item.getDisabled());

	const scrollHighlightedIntoView = () => {
		const item = orderedItems.find((entry) => entry.id === highlightedId);
		item?.getElement()?.scrollIntoView({ block: "nearest", inline: "nearest" });
	};

	const setHighlighted = (itemId: string) => {
		const item = items.find((entry) => entry.id === itemId);
		if (!item || item.getDisabled()) return;

		highlightedId = itemId;
		if (open) scrollHighlightedIntoView();
	};

	const prepareHighlight = (intent: SelectHighlightIntent = "selected-or-first") => {
		const available = enabledItems();
		if (available.length === 0) {
			highlightedId = null;
			return;
		}

		if (intent === "first") highlightedId = available[0]?.id ?? null;
		else if (intent === "last") highlightedId = available.at(-1)?.id ?? null;
		else {
			const selected = selectedItems.find((item) => !item.getDisabled());
			highlightedId =
				selected?.id ?? (intent === "selected-or-last" ? available.at(-1)?.id : available[0]?.id) ?? null;
		}
	};

	const requestOpenChange = (
		nextOpen: boolean,
		intent: SelectHighlightIntent = "selected-or-first",
		shouldRestoreFocus = false
	) => {
		if (nextOpen && disabled) return;

		if (open === nextOpen) {
			if (nextOpen) prepareHighlight(intent);
			return;
		}

		if (nextOpen) {
			prepareHighlight(intent);
			restoreFocus = false;
		} else {
			restoreFocus = shouldRestoreFocus;
			typeaheadBuffer = "";
			if (typeaheadTimer) clearTimeout(typeaheadTimer);
			typeaheadTimer = undefined;
		}

		open = nextOpen;
		reportedOpen = nextOpen;
		onOpenChange?.(nextOpen);
	};

	const notifyValueChange = (
		nextValue: SelectItemValue | null | SelectItemValue[],
		source: SelectChangeSource
	) => {
		if (type === "single") {
			(onValueChange as SelectSingleProps["onValueChange"])?.(nextValue as SelectItemValue | null, source);
		} else {
			(onValueChange as SelectMultipleProps["onValueChange"])?.(nextValue as SelectItemValue[], source);
		}
	};

	const selectItem = (itemValue: SelectItemValue, source: SelectChangeSource) => {
		if (disabled) return;
		const item = items.find((entry) => entry.value === itemValue);
		if (!item || item.getDisabled()) return;

		if (type === "single") {
			const previousValue = typeof value === "string" ? value : null;
			const nextValue = previousValue === itemValue && allowDeselect && !required ? null : itemValue;
			if (previousValue === nextValue) {
				if (shouldCloseOnSelect) requestOpenChange(false, "selected-or-first", true);
				return;
			}

			value = nextValue;
			notifyValueChange(nextValue, source);
		} else {
			const previousValues = selectedItems.map((entry) => entry.value);
			const selectedSet = new Set(previousValues);
			if (selectedSet.has(itemValue)) selectedSet.delete(itemValue);
			else selectedSet.add(itemValue);
			const nextValues = orderedItems.filter((entry) => selectedSet.has(entry.value)).map((entry) => entry.value);
			if (stringArraysEqual(previousValues, nextValues)) return;

			value = nextValues;
			notifyValueChange(nextValues, source);
		}

		highlightedId = item.id;
		if (shouldCloseOnSelect) requestOpenChange(false, "selected-or-first", true);
	};

	const moveHighlight = (offset: number) => {
		const available = enabledItems();
		if (available.length === 0) return;

		let index = available.findIndex((item) => item.id === highlightedId);
		if (index === -1) index = offset > 0 ? -1 : available.length;
		let nextIndex = index + offset;

		if (nextIndex < 0 || nextIndex >= available.length) {
			if (!loop) return;
			nextIndex = (nextIndex + available.length) % available.length;
		}

		const item = available[nextIndex];
		if (item) setHighlighted(item.id);
	};

	const handleTypeahead = (key: string) => {
		if (!typeahead) return;
		if (typeaheadTimer) clearTimeout(typeaheadTimer);

		typeaheadBuffer += normalizeSelectText(key);
		typeaheadTimer = setTimeout(() => {
			typeaheadBuffer = "";
			typeaheadTimer = undefined;
		}, 500);

		const available = enabledItems();
		const currentIndex = available.findIndex((item) => item.id === highlightedId);
		const candidates = [...available.slice(currentIndex + 1), ...available.slice(0, currentIndex + 1)];
		const repeatedCharacter = typeaheadBuffer.length > 1 && new Set(typeaheadBuffer).size === 1;
		const query = repeatedCharacter ? typeaheadBuffer[0] : typeaheadBuffer;
		const match = candidates.find((item) => normalizeSelectText(item.getTextValue()).startsWith(query));
		if (match) setHighlighted(match.id);
	};

	const reconcileItems = () => {
		const sorted = sortItems(items);
		if (sorted.some((item, index) => item !== items[index])) items = sorted;
	};

	setSelectContext({
		get closeOnSelect() {
			return shouldCloseOnSelect;
		},
		get currentValue() {
			return currentValue;
		},
		get defaultDescriptionId() {
			return defaultDescriptionId;
		},
		get defaultErrorId() {
			return defaultErrorId;
		},
		get defaultLabelId() {
			return defaultLabelId;
		},
		get descriptionId() {
			return descriptionIdGetters.at(-1)?.();
		},
		get disabled() {
			return disabled;
		},
		get empty() {
			return empty;
		},
		get errorId() {
			return errorIdGetters.at(-1)?.();
		},
		get highlightedId() {
			return highlightedId;
		},
		get invalid() {
			return invalid;
		},
		get labelId() {
			return labelIdGetters.at(-1)?.();
		},
		get listboxId() {
			return listboxIdGetters.at(-1)?.() ?? defaultListboxId;
		},
		get loop() {
			return loop;
		},
		get open() {
			return open;
		},
		get required() {
			return required;
		},
		get selectedLabels() {
			return selectedLabels;
		},
		get triggerId() {
			return triggerIdGetters.at(-1)?.() ?? defaultTriggerId;
		},
		get type() {
			return type;
		},
		get typeahead() {
			return typeahead;
		},
		consumeRestoreFocus() {
			const value = restoreFocus;
			restoreFocus = false;
			return value;
		},
		focusViewport() {
			const viewport = viewportGetters.map((getter) => getter()).find((element) => element?.isConnected);
			if (!viewport) return;

			try {
				viewport.focus({ preventScroll: true });
			} catch {
				viewport.focus();
			}
			scrollHighlightedIntoView();
		},
		getTrigger() {
			if (lastTrigger?.isConnected) return lastTrigger;
			return triggerGetters.map((getter) => getter()).find((element) => element?.isConnected) ?? null;
		},
		getViewport() {
			return viewportGetters.map((getter) => getter()).find((element) => element?.isConnected) ?? null;
		},
		handleTypeahead,
		isItemSelected(itemValue) {
			return selectedItems.some((item) => item.value === itemValue);
		},
		moveHighlight,
		reconcileItems,
		registerDescriptionId(getId) {
			return registerId(
				() => descriptionIdGetters,
				getId,
				(getters) => (descriptionIdGetters = getters)
			);
		},
		registerErrorId(getId) {
			return registerId(
				() => errorIdGetters,
				getId,
				(getters) => (errorIdGetters = getters)
			);
		},
		registerItem(item) {
			const duplicate = items.find((entry) => entry.value === item.value);
			if (duplicate) throw new Error(`SelectItem values must be unique. Duplicate value: "${item.value}".`);

			items = [...items, item];
			return () => {
				items = items.filter((entry) => entry !== item);
				if (highlightedId === item.id) highlightedId = open ? enabledItems()[0]?.id ?? null : null;
			};
		},
		registerLabelId(getId) {
			return registerId(
				() => labelIdGetters,
				getId,
				(getters) => (labelIdGetters = getters)
			);
		},
		registerListboxId(getId) {
			return registerId(
				() => listboxIdGetters,
				getId,
				(getters) => (listboxIdGetters = getters)
			);
		},
		registerTrigger(getElement) {
			return registerGetter(triggerGetters, getElement);
		},
		registerTriggerId(getId) {
			return registerId(
				() => triggerIdGetters,
				getId,
				(getters) => (triggerIdGetters = getters)
			);
		},
		registerViewport(getElement) {
			return registerGetter(viewportGetters, getElement);
		},
		requestOpenChange(nextOpen, intent, shouldRestoreFocus) {
			if (nextOpen) {
				const trigger = document.activeElement;
				if (trigger instanceof HTMLButtonElement) lastTrigger = trigger;
			}
			requestOpenChange(nextOpen, intent, shouldRestoreFocus);
		},
		restoreTriggerFocus() {
			const trigger = lastTrigger?.isConnected
				? lastTrigger
				: triggerGetters.map((getter) => getter()).find((element) => element?.isConnected);
			trigger?.focus({ preventScroll: true });
		},
		selectHighlighted(source) {
			const item = items.find((entry) => entry.id === highlightedId);
			if (item) selectItem(item.value, source);
		},
		selectItem,
		setHighlighted,
		synchronizeProgrammaticOpen() {
			if (open === reportedOpen) return;

			reportedOpen = open;
			if (open) {
				prepareHighlight();
				restoreFocus = false;
			} else {
				restoreFocus = true;
				highlightedId = null;
			}
			onOpenChange?.(open);
		},
		valueState(): SelectValueState {
			return {
				value: currentValue,
				labels: selectedLabels,
				placeholder: empty
			};
		}
	});

	onDestroy(() => {
		if (typeaheadTimer) clearTimeout(typeaheadTimer);
	});
</script>

<div
	{...restProps}
	bind:this={ref}
	{id}
	class={["select-root", className]}
	data-component="select"
	data-select-root
	data-type={type}
	data-state={open ? "open" : "closed"}
	data-disabled={disabled ? "" : undefined}
	data-invalid={invalid ? "" : undefined}
	data-required={required ? "" : undefined}
	data-empty={empty ? "" : undefined}
>
	{@render children()}
	{#if name && type === "single" && typeof currentValue === "string"}
		<input type="hidden" {name} value={currentValue} {disabled} />
	{:else if name && type === "multiple" && Array.isArray(currentValue)}
		{#each currentValue as selectedValue}
			<input type="hidden" {name} value={selectedValue} {disabled} />
		{/each}
	{/if}
</div>

<style>
	.select-root {
		display: grid;
		width: 100%;
		min-width: 0;
		gap: 0.5rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
	}
</style>
