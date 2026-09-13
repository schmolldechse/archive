<script lang="ts">
	import { onDestroy } from "svelte";

	import {
		getDropdownMenuContentContext,
		getDropdownMenuRootContext,
		setDropdownMenuSubContext,
		type DropdownMenuContentContext
	} from "./dropdown-menu-context.svelte";
	import type { DropdownMenuFocusIntent, DropdownMenuSubProps, MenuOpenReason } from "./types";

	let {
		open = $bindable(false),
		onOpenChange,
		children
	}: DropdownMenuSubProps = $props();

	const root = getDropdownMenuRootContext("DropdownMenuSub");
	const parentMenu = getDropdownMenuContentContext("DropdownMenuSub");
	const generatedId = $props.id();
	const submenuId = `${generatedId}-submenu`;
	const defaultContentId = `${generatedId}-content`;
	const defaultTriggerId = `${generatedId}-trigger`;
	let contentIdGetters = $state<Array<() => string>>([]);
	let triggerIdGetters = $state<Array<() => string>>([]);
	const contentGetters: Array<() => HTMLDivElement | null> = [];
	const triggerGetters: Array<() => HTMLButtonElement | null> = [];
	const menuGetters: Array<() => DropdownMenuContentContext> = [];
	let focusIntent: DropdownMenuFocusIntent | null = null;
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
		if (nextOpen && root.disabled) return;
		if (nextFocusIntent) focusIntent = nextFocusIntent;

		if (open === nextOpen) {
			if (nextOpen && nextFocusIntent) focusMenu(nextFocusIntent);
			return;
		}

		if (nextOpen) parentMenu.closeSubmenus(reason, submenuId);
		else {
			currentMenu()?.closeSubmenus(reason);
			currentMenu()?.reset();
			restoreFocus = shouldRestoreFocus;
		}

		open = nextOpen;
		reportedOpen = nextOpen;
		onOpenChange?.(nextOpen, reason);
	};

	const registration = {
		id: submenuId,
		close(reason: MenuOpenReason) {
			requestOpenChange(false, reason);
		},
		open(reason: MenuOpenReason, nextFocusIntent?: DropdownMenuFocusIntent) {
			requestOpenChange(true, reason, nextFocusIntent);
		}
	};
	const unregisterParent = parentMenu.registerSubmenu(registration);
	const unregisterRoot = root.registerSubmenu(registration);

	onDestroy(() => {
		unregisterParent();
		unregisterRoot();
	});

	setDropdownMenuSubContext({
		get contentId() {
			return contentIdGetters.at(-1)?.() ?? defaultContentId;
		},
		get id() {
			return submenuId;
		},
		get open() {
			return open;
		},
		get triggerId() {
			return triggerIdGetters.at(-1)?.() ?? defaultTriggerId;
		},
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
			const trigger = triggerGetters.map((getter) => getter()).find((element) => element?.isConnected);
			trigger?.focus({ preventScroll: true });
		},
		synchronizeProgrammaticChange() {
			if (open === reportedOpen) return;

			reportedOpen = open;
			if (open) parentMenu.closeSubmenus("programmatic", submenuId);
			else {
				currentMenu()?.closeSubmenus("programmatic");
				currentMenu()?.reset();
				restoreFocus = false;
			}
			onOpenChange?.(open, "programmatic");
		}
	});
</script>

{@render children()}
