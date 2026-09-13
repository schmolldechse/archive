<script lang="ts">
	import type { Attachment } from "svelte/attachments";

	import { getTabsValueIdSegment, mergeClasses } from "./helpers";
	import { setTabsContext, type TabsPanelRegistration, type TabsTriggerRegistration } from "./tabs-context.svelte";
	import type { TabsRootProps } from "./types";

	const generatedId = $props.id();

	let {
		value = $bindable(),
		onValueChange,
		orientation = "horizontal",
		activationMode = "automatic",
		loop = true,
		disabled = false,
		children,
		ref = $bindable(null),
		class: className,
		...restProps
	}: TabsRootProps = $props();

	let focusedValue = $state<string | undefined>();
	let triggerRegistrations = $state.raw<TabsTriggerRegistration[]>([]);
	let panelRegistrations = $state.raw<TabsPanelRegistration[]>([]);
	let validationElement: HTMLDivElement | null = null;
	let validationQueued = false;
	const shownWarnings = new Set<string>();

	const getDefaultTriggerId = (targetValue: string): string => `${generatedId}-trigger-${getTabsValueIdSegment(targetValue)}`;
	const getDefaultPanelId = (targetValue: string): string => `${generatedId}-panel-${getTabsValueIdSegment(targetValue)}`;

	const getOrderedTriggers = (): TabsTriggerRegistration[] => {
		const registrationOrder = new Map(triggerRegistrations.map((registration, index) => [registration, index]));

		return [...triggerRegistrations].sort((left, right) => {
			if (left.element?.isConnected && right.element?.isConnected) {
				const position = left.element.compareDocumentPosition(right.element);

				if (position & 4) return -1;
				if (position & 2) return 1;
			}

			return (registrationOrder.get(left) ?? 0) - (registrationOrder.get(right) ?? 0);
		});
	};

	const getEnabledTriggers = (): TabsTriggerRegistration[] =>
		getOrderedTriggers().filter((registration) => !registration.disabled);

	const getEffectiveValue = (): string | undefined => {
		const enabledTriggers = getEnabledTriggers();

		if (value !== undefined && enabledTriggers.some((registration) => registration.value === value)) {
			return value;
		}

		return enabledTriggers[0]?.value;
	};

	const getTabStopValue = (): string | undefined => {
		if (
			activationMode === "manual" &&
			focusedValue !== undefined &&
			getEnabledTriggers().some((registration) => registration.value === focusedValue)
		) {
			return focusedValue;
		}

		return getEffectiveValue();
	};

	const getTriggerId = (targetValue: string): string => getDefaultTriggerId(targetValue);
	const getPanelId = (targetValue: string): string => getDefaultPanelId(targetValue);

	const isTriggerDisabled = (targetValue: string, triggerDisabled: boolean): boolean => {
		const registeredDisabled = triggerRegistrations.find((registration) => registration.value === targetValue)?.disabled;

		return disabled || triggerDisabled || registeredDisabled === true;
	};

	const activate = (targetValue: string, triggerDisabled: boolean): void => {
		if (isTriggerDisabled(targetValue, triggerDisabled) || value === targetValue) return;

		value = targetValue;
		onValueChange?.(targetValue);
	};

	const focusTrigger = (targetValue: string, triggerDisabled: boolean): void => {
		if (isTriggerDisabled(targetValue, triggerDisabled)) return;

		focusedValue = targetValue;

		if (activationMode === "automatic") activate(targetValue, triggerDisabled);
	};

	const blurTrigger = (nextTarget: EventTarget | null): void => {
		const focusRemainsInList = getOrderedTriggers().some((registration) => registration.element === nextTarget);

		if (!focusRemainsInList) focusedValue = undefined;
	};

	const navigate = (event: KeyboardEvent, currentValue: string): void => {
		if (event.altKey || event.ctrlKey || event.metaKey) return;

		const enabledTriggers = getEnabledTriggers();
		if (enabledTriggers.length === 0) return;

		const currentIndex = enabledTriggers.findIndex((registration) => registration.value === currentValue);
		let nextIndex: number;

		switch (event.key) {
			case "ArrowLeft":
				if (orientation !== "horizontal") return;
				nextIndex = currentIndex === -1 ? enabledTriggers.length - 1 : currentIndex - 1;
				break;
			case "ArrowRight":
				if (orientation !== "horizontal") return;
				nextIndex = currentIndex === -1 ? 0 : currentIndex + 1;
				break;
			case "ArrowUp":
				if (orientation !== "vertical") return;
				nextIndex = currentIndex === -1 ? enabledTriggers.length - 1 : currentIndex - 1;
				break;
			case "ArrowDown":
				if (orientation !== "vertical") return;
				nextIndex = currentIndex === -1 ? 0 : currentIndex + 1;
				break;
			case "Home":
				nextIndex = 0;
				break;
			case "End":
				nextIndex = enabledTriggers.length - 1;
				break;
			default:
				return;
		}

		if (nextIndex < 0 || nextIndex >= enabledTriggers.length) {
			if (!loop) {
				event.preventDefault();
				return;
			}
			nextIndex = (nextIndex + enabledTriggers.length) % enabledTriggers.length;
		}

		event.preventDefault();
		enabledTriggers[nextIndex]?.element?.focus();
	};

	const validateRegistrations = (): void => {
		const problems = new Map<string, string>();
		const triggerCounts = new Map<string, number>();
		const panelCounts = new Map<string, number>();

		for (const registration of triggerRegistrations) {
			triggerCounts.set(registration.value, (triggerCounts.get(registration.value) ?? 0) + 1);
		}

		for (const registration of panelRegistrations) {
			panelCounts.set(registration.value, (panelCounts.get(registration.value) ?? 0) + 1);
		}

		for (const [registeredValue, count] of triggerCounts) {
			const describedValue = JSON.stringify(registeredValue);

			if (count > 1) {
				problems.set(
					`duplicate-trigger:${registeredValue}`,
					`TabsTrigger values must be unique. Duplicate value: ${describedValue}.`
				);
			}

			if (!panelCounts.has(registeredValue)) {
				problems.set(`missing-panel:${registeredValue}`, `TabsTrigger value ${describedValue} has no matching TabsPanel.`);
			}
		}

		for (const [registeredValue, count] of panelCounts) {
			const describedValue = JSON.stringify(registeredValue);

			if (count > 1) {
				problems.set(
					`duplicate-panel:${registeredValue}`,
					`TabsPanel values must be unique. Duplicate value: ${describedValue}.`
				);
			}

			if (!triggerCounts.has(registeredValue)) {
				problems.set(`missing-trigger:${registeredValue}`, `TabsPanel value ${describedValue} has no matching TabsTrigger.`);
			}
		}

		if (value !== undefined) {
			const matchingTriggers = triggerRegistrations.filter((registration) => registration.value === value);
			const describedValue = JSON.stringify(value);

			if (matchingTriggers.length === 0) {
				problems.set(
					`invalid-value:missing:${value}`,
					`TabsRoot value ${describedValue} has no matching TabsTrigger; the first enabled trigger is rendered as active.`
				);
			} else if (matchingTriggers.every((registration) => registration.disabled)) {
				problems.set(
					`invalid-value:disabled:${value}`,
					`TabsRoot value ${describedValue} references a disabled TabsTrigger; the first enabled trigger is rendered as active.`
				);
			}
		}

		for (const warningKey of shownWarnings) {
			if (!problems.has(warningKey)) shownWarnings.delete(warningKey);
		}

		for (const [warningKey, message] of problems) {
			if (shownWarnings.has(warningKey)) continue;
			shownWarnings.add(warningKey);
			console.warn(message);
		}
	};

	const queueRegistrationValidation = (): void => {
		if (!import.meta.env.DEV || validationQueued) return;

		validationQueued = true;
		queueMicrotask(() => {
			validationQueued = false;

			if (validationElement?.isConnected) validateRegistrations();
		});
	};

	const registerTrigger = (registration: TabsTriggerRegistration): (() => void) => {
		triggerRegistrations = [...triggerRegistrations, registration];

		if (value === undefined && !registration.disabled) value = registration.value;

		queueRegistrationValidation();

		return () => {
			triggerRegistrations = triggerRegistrations.filter((entry) => entry !== registration);
			queueRegistrationValidation();
		};
	};

	const registerPanel = (registration: TabsPanelRegistration): (() => void) => {
		panelRegistrations = [...panelRegistrations, registration];
		queueRegistrationValidation();

		return () => {
			panelRegistrations = panelRegistrations.filter((entry) => entry !== registration);
			queueRegistrationValidation();
		};
	};

	const observeComposition: Attachment<HTMLDivElement> = (element) => {
		void value;
		void triggerRegistrations;
		void panelRegistrations;

		validationElement = element;
		queueRegistrationValidation();

		const observer = new MutationObserver(queueRegistrationValidation);
		observer.observe(element, {
			attributes: true,
			attributeFilter: ["data-disabled", "data-state", "data-value"],
			childList: true,
			subtree: true
		});

		return () => {
			observer.disconnect();
			if (validationElement === element) validationElement = null;
		};
	};

	setTabsContext({
		get activationMode() {
			return activationMode;
		},
		get activeValue() {
			return getEffectiveValue();
		},
		get disabled() {
			return disabled;
		},
		get loop() {
			return loop;
		},
		get orientation() {
			return orientation;
		},
		activate,
		blurTrigger,
		focusTrigger,
		getDefaultPanelId,
		getDefaultTriggerId,
		getPanelId,
		getTriggerId,
		isSelected(targetValue) {
			return getEffectiveValue() === targetValue;
		},
		isTabStop(targetValue, triggerDisabled) {
			return !isTriggerDisabled(targetValue, triggerDisabled) && getTabStopValue() === targetValue;
		},
		isTriggerDisabled,
		navigate,
		registerPanel,
		registerTrigger
	});
</script>

<div
	{...restProps}
	bind:this={ref}
	class={mergeClasses("tabs-root", className)}
	data-component="tabs"
	data-orientation={orientation}
	data-disabled={disabled ? "" : undefined}
	{@attach import.meta.env.DEV && observeComposition}
>
	{@render children()}
</div>

<style>
	.tabs-root {
		width: 100%;
		min-width: 0;
		color: var(--reading-ink);
		font-family: var(--font-interface);
	}

	.tabs-root[data-orientation="vertical"] {
		display: flex;
		align-items: flex-start;
		gap: 1.5rem;
	}

	.tabs-root[data-orientation="vertical"] > :global([data-tabs-list]) {
		width: min(14rem, 42%);
		flex: none;
	}

	.tabs-root[data-orientation="vertical"] > :global([data-tabs-panel]) {
		min-width: 0;
		flex: 1;
		padding-block-start: 0;
	}

	@media (max-width: 32rem) {
		.tabs-root[data-orientation="vertical"] {
			flex-direction: column;
		}

		.tabs-root[data-orientation="vertical"] > :global([data-tabs-list]) {
			width: 100%;
		}

		.tabs-root[data-orientation="vertical"] > :global([data-tabs-panel]) {
			width: 100%;
			padding-block-start: 1.5rem;
		}
	}
</style>
