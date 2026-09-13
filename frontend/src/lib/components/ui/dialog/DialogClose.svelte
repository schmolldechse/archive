<script lang="ts">
	import Button from "../Button.svelte";
	import { getDialogContext } from "./dialog-context.svelte";
	import type { DialogCloseProps } from "./types";

	let {
		disabled = false,
		children,
		ref = $bindable(null),
		class: className,
		onclick,
		...restProps
	}: DialogCloseProps = $props();

	const dialog = getDialogContext("DialogClose");

	const handleClick: NonNullable<DialogCloseProps["onclick"]> = (event) => {
		onclick?.(event);
		if (event.defaultPrevented || disabled) return;

		dialog.requestOpenChange(false, "close");
	};
</script>

<Button
	{...restProps}
	bind:ref
	type="button"
	{disabled}
	class={["dialog-close", className]}
	data-dialog-close
	onclick={handleClick}
>
	{@render children()}
</Button>
