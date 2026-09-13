<script lang="ts">
	import { onDestroy } from "svelte";

	import Button from "../Button.svelte";
	import { getDialogContext } from "./dialog-context.svelte";
	import type { DialogTriggerProps } from "./types";

	let {
		disabled = false,
		children,
		ref = $bindable(null),
		class: className,
		onclick,
		...restProps
	}: DialogTriggerProps = $props();

	const dialog = getDialogContext("DialogTrigger");
	const unregisterTrigger = dialog.registerTrigger(() => ref);

	onDestroy(unregisterTrigger);

	const handleClick: NonNullable<DialogTriggerProps["onclick"]> = (event) => {
		onclick?.(event);
		if (event.defaultPrevented || disabled) return;

		dialog.setLastTrigger(event.currentTarget);
		dialog.requestOpenChange(true, "trigger");
	};
</script>

<Button
	{...restProps}
	bind:ref
	type="button"
	{disabled}
	class={["dialog-trigger", className]}
	aria-haspopup="dialog"
	aria-expanded={dialog.open}
	aria-controls={dialog.contentId}
	data-dialog-trigger
	data-state={dialog.open ? "open" : "closed"}
	onclick={handleClick}
>
	{@render children()}
</Button>
