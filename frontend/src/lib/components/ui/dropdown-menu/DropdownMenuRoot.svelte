<script lang="ts">
	import {
		setDropdownMenuRootContext,
		type DropdownMenuContentContext
	} from "./dropdown-menu-context.svelte";
	import type { DropdownMenuFocusIntent, DropdownMenuRootProps, MenuOpenReason } from "./types";

	let {
		open = $bindable(false),
		onOpenChange,
		loop = false,
		typeahead = true,
		disabled = false,
		children
	}: DropdownMenuRootProps = $props();

	const generatedId = $props.id();
	const defaultContentId = `${generatedId}-content`;
	const defaultTriggerId = `${generatedId}-trigger`;
	let contentIdGetters = $state<Array<() => string>>([]);
	let triggerIdGetters = $state<Array<() => string>>([]);
	const contentGetters: Array<() => HTMLDivElement | null> = [];
	const triggerGetters: Array<() => HTMLButtonElement | null> = [];
	const menuGetters: Array<() => DropdownMenuContentContext> = [];
	const submenus: Array<{ close(reason: MenuOpenReason): void }> = [];
	let lastTrigger: HTMLButtonElement | null = null;
	let focusIntent: DropdownMenuFocusIntent | null = open ? "first" : null;
	let restoreFocus = false;
	let reportedOpen = open;

	const registerGetter = <T,>(getters: Array<() => T>, getter: () => T) => {
		getters.push(getter);
		return () => {
			const index = getters.indexOf(getter);
			if (index !== -1) getters.splice(index, 1);
		};
	};

	const registerReactiveId = (
		getGetters: () => Array<() => string>,
		getter: () => string,
		assign: (value: Array<() => string>) => void
	) => {
		assign([...getGetters(), getter]);
		return () => assign(getGetters().filter((entry) => entry !== getter));
	};

	const currentMenu = () => menuGetters.at(-1)?.();

	const closeSubmenus = (reason: MenuOpenReason) => {
		for (const submenu of submenus) submenu.close(reason);
	};

	const focusMenu = (intent: DropdownMenuFocusIntent) => {
		const menu = currentMenu();
		if (!menu) return;
		if (intent === "first") menu.focusFirst();
		else menu.focusLast();
	};

	const requestOpenChange = (
		nextOpen: boolean,
		reason: MenuOpenReason,
		nextFocusIntent?: DropdownMenuFocusIntent,
		shouldRestoreFocus = false
	) => {
		if (nextOpen && disabled) return;
		if (nextFocusIntent) focusIntent = nextFocusIntent;

		if (open === nextOpen) {
			if (nextOpen && nextFocusIntent) focusMenu(nextFocusIntent);
			return;
		}

		if (!nextOpen) {
			closeSubmenus(reason);
			currentMenu()?.reset();
			restoreFocus = shouldRestoreFocus;
		}

		open = nextOpen;
		reportedOpen = nextOpen;
		onOpenChange?.(nextOpen, reason);
	};

	setDropdownMenuRootContext({
		get contentId() {
			return contentIdGetters.at(-1)?.() ?? defaultContentId;
		},
		get disabled() {
			return disabled;
		},
		get loop() {
			return loop;
		},
		get open() {
			return open;
		},
		get triggerId() {
			return triggerIdGetters.at(-1)?.() ?? defaultTriggerId;
		},
		get typeahead() {
			return typeahead;
		},
		closeSubmenus,
		consumeFocusIntent() {
			const value = focusIntent;
			focusIntent = null;
			return value;
		},
		consumeRestoreFocus() {
			const value = restoreFocus;
			restoreFocus = false;
			return value;
		},
		getContent() {
			return contentGetters.map((getter) => getter()).find((element) => element?.isConnected) ?? null;
		},
		getTrigger() {
			if (lastTrigger?.isConnected) return lastTrigger;
			return triggerGetters.map((getter) => getter()).find((element) => element?.isConnected) ?? null;
		},
		registerContent(getElement) {
			return registerGetter(contentGetters, getElement);
		},
		registerContentId(getId) {
			return registerReactiveId(
				() => contentIdGetters,
				getId,
				(value) => (contentIdGetters = value)
			);
		},
		registerMenu(getMenu) {
			return registerGetter(menuGetters, getMenu);
		},
		registerSubmenu(submenu) {
			submenus.push(submenu);
			return () => {
				const index = submenus.indexOf(submenu);
				if (index !== -1) submenus.splice(index, 1);
			};
		},
		registerTrigger(getElement) {
			return registerGetter(triggerGetters, getElement);
		},
		registerTriggerId(getId) {
			return registerReactiveId(
				() => triggerIdGetters,
				getId,
				(value) => (triggerIdGetters = value)
			);
		},
		requestOpenChange,
		restoreTriggerFocus() {
			const trigger = lastTrigger?.isConnected ? lastTrigger : triggerGetters.map((getter) => getter()).find(Boolean);
			trigger?.focus({ preventScroll: true });
		},
		setLastTrigger(element) {
			lastTrigger = element;
		},
		synchronizeProgrammaticChange() {
			if (open === reportedOpen) return;

			reportedOpen = open;
			if (open) focusIntent = "first";
			else {
				closeSubmenus("programmatic");
				currentMenu()?.reset();
				restoreFocus = false;
			}
			onOpenChange?.(open, "programmatic");
		}
	});
</script>

{@render children()}
