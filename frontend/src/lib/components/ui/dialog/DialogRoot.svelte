<script lang="ts">
	import { setDialogContext } from "./dialog-context.svelte";
	import type { DialogOpenReason, DialogRootProps } from "./types";

	let { open = $bindable(false), modal = true, onOpenChange, children }: DialogRootProps = $props();

	const generatedId = $props.id();
	const defaultContentId = `${generatedId}-content`;
	let contentIdGetters = $state<Array<() => string>>([]);
	let titleIdGetters = $state<Array<() => string>>([]);
	let descriptionIdGetters = $state<Array<() => string>>([]);
	const triggerGetters: Array<() => HTMLButtonElement | null> = [];
	let lastTrigger: HTMLButtonElement | null = null;
	let reportedOpen = open;

	const registerId = (
		getGetters: () => Array<() => string>,
		getId: () => string,
		assign: (value: Array<() => string>) => void
	) => {
		assign([...getGetters(), getId]);

		return () => assign(getGetters().filter((entry) => entry !== getId));
	};

	const requestOpenChange = (nextOpen: boolean, reason: DialogOpenReason) => {
		if (open === nextOpen) return;

		open = nextOpen;
		reportedOpen = nextOpen;
		onOpenChange?.(nextOpen, reason);
	};

	setDialogContext({
		get contentId() {
			return contentIdGetters.at(-1)?.() ?? defaultContentId;
		},
		get descriptionId() {
			return descriptionIdGetters.at(-1)?.();
		},
		get modal() {
			return modal;
		},
		get open() {
			return open;
		},
		get titleId() {
			return titleIdGetters.at(-1)?.();
		},
		getTrigger() {
			if (lastTrigger?.isConnected) return lastTrigger;
			return triggerGetters.map((getElement) => getElement()).find((element) => element?.isConnected) ?? null;
		},
		registerContentId(getId) {
			return registerId(
				() => contentIdGetters,
				getId,
				(value) => (contentIdGetters = value)
			);
		},
		registerDescriptionId(getId) {
			return registerId(
				() => descriptionIdGetters,
				getId,
				(value) => (descriptionIdGetters = value)
			);
		},
		registerTitleId(getId) {
			return registerId(
				() => titleIdGetters,
				getId,
				(value) => (titleIdGetters = value)
			);
		},
		registerTrigger(getElement) {
			triggerGetters.push(getElement);

			return () => {
				const index = triggerGetters.indexOf(getElement);
				if (index !== -1) triggerGetters.splice(index, 1);
			};
		},
		requestOpenChange,
		setLastTrigger(element) {
			lastTrigger = element;
		},
		synchronizeProgrammaticChange() {
			if (open === reportedOpen) return;

			reportedOpen = open;
			onOpenChange?.(open, "programmatic");
		}
	});
</script>

{@render children()}
