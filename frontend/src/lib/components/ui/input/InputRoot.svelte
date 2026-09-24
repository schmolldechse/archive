<script lang="ts">
	import type { Attachment } from "svelte/attachments";

	import { setInputContext } from "./input-context.svelte";
	import type { InputRootProps, InputState } from "./types";

	const generatedId = $props.id();

	let {
		id = generatedId,
		disabled = false,
		required = false,
		invalid = false,
		hasDescription = false,
		children,
		ref = $bindable(null),
		class: className,
		...restProps
	}: InputRootProps = $props();

	const controlId = $derived(`${id}-control`);
	const descriptionId = $derived(`${id}-description`);
	const errorId = $derived(`${id}-error`);
	const state = $derived<InputState>(disabled ? "disabled" : invalid ? "invalid" : "default");

	setInputContext({
		get controlId() {
			return controlId;
		},
		get descriptionId() {
			return descriptionId;
		},
		get disabled() {
			return disabled;
		},
		get errorId() {
			return errorId;
		},
		get hasDescription() {
			return hasDescription;
		},
		get invalid() {
			return invalid;
		},
		get required() {
			return required;
		},
		get state() {
			return state;
		}
	});

	const validateComposition: Attachment<HTMLDivElement> = (element) => {
		const shownWarnings = new Set<string>();
		let attached = true;

		const hasOwnPart = (selector: string): boolean =>
			Array.from(element.querySelectorAll(selector)).some((part) => part.closest('[data-component="input"]') === element);

		const warn = (key: string, missing: boolean, message: string): void => {
			if (!missing) {
				shownWarnings.delete(key);
				return;
			}

			if (shownWarnings.has(key)) return;
			shownWarnings.add(key);
			console.warn(message);
		};

		$effect(() => {
			const expectsDescription = hasDescription;
			const expectsError = invalid;

			queueMicrotask(() => {
				if (!attached || !element.isConnected) return;

				warn("label", !hasOwnPart("[data-input-label]"), "InputRoot requires an InputLabel.");
				warn("control", !hasOwnPart("[data-input-control]"), "InputRoot requires an InputControl or InputTextarea.");
				warn(
					"description",
					expectsDescription && !hasOwnPart("[data-input-description]"),
					"InputRoot hasDescription is true, but no InputDescription is rendered."
				);
				warn(
					"error",
					expectsError && !hasOwnPart("[data-input-error]"),
					"InputRoot invalid is true, but no InputError is rendered."
				);
			});
		});

		return () => {
			attached = false;
		};
	};
</script>

<div
	{...restProps}
	bind:this={ref}
	{id}
	class={["input-root", className]}
	data-component="input"
	data-state={state}
	data-disabled={disabled ? "" : undefined}
	data-required={required ? "" : undefined}
	{@attach import.meta.env.DEV && validateComposition}
>
	{@render children()}
</div>

<style>
	.input-root {
		display: grid;
		width: 100%;
		min-width: 0;
		gap: 0.5rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
	}
</style>
