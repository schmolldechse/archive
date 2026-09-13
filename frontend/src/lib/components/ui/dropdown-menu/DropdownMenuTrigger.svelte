<script lang="ts">
	import { onDestroy } from "svelte";

	import Button from "../Button.svelte";
	import { getDropdownMenuRootContext } from "./dropdown-menu-context.svelte";
	import type { DropdownMenuTriggerProps } from "./types";

	const generatedId = $props.id();

	let {
		disabled = false,
		children,
		ref = $bindable(null),
		class: className,
		id,
		onclick,
		onkeydown,
		...restProps
	}: DropdownMenuTriggerProps = $props();

	const root = getDropdownMenuRootContext("DropdownMenuTrigger");
	const unregisterTrigger = root.registerTrigger(() => ref);
	const unregisterId = root.registerTriggerId(() => id ?? generatedId);
	const isDisabled = $derived(root.disabled || disabled);

	onDestroy(() => {
		unregisterTrigger();
		unregisterId();
	});

	const handleClick: NonNullable<DropdownMenuTriggerProps["onclick"]> = (event) => {
		onclick?.(event);
		if (event.defaultPrevented || isDisabled) return;

		root.setLastTrigger(event.currentTarget);
		root.requestOpenChange(!root.open, "trigger", root.open ? undefined : "first");
	};

	const handleKeydown: NonNullable<DropdownMenuTriggerProps["onkeydown"]> = (event) => {
		onkeydown?.(event);
		if (event.defaultPrevented || isDisabled) return;

		if (event.key !== "ArrowDown" && event.key !== "ArrowUp") return;

		event.preventDefault();
		root.setLastTrigger(event.currentTarget);
		root.requestOpenChange(true, "keyboard", event.key === "ArrowDown" ? "first" : "last");
	};
</script>

<Button
	{...restProps}
	bind:ref
	id={id ?? generatedId}
	type="button"
	disabled={isDisabled}
	class={["dropdown-menu-trigger", className]}
	aria-haspopup="menu"
	aria-expanded={root.open}
	aria-controls={root.contentId}
	data-dropdown-menu-trigger
	data-state={root.open ? "open" : "closed"}
	onclick={handleClick}
	onkeydown={handleKeydown}
>
	{@render children()}
</Button>
