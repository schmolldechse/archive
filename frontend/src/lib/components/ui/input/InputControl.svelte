<script lang="ts">
	import { getInputContext } from "./input-context.svelte";
	import type { InputControlProps } from "./types";

	let {
		type = "text",
		value = $bindable(),
		onFilesChange,
		onchange,
		ref = $bindable(null),
		class: className,
		"aria-describedby": consumerDescribedBy,
		"data-value-kind": valueKind = "text",
		...restProps
	}: InputControlProps = $props();

	const input = getInputContext("InputControl");
	let dragging = $state(false);
	let selectedFiles = $state<string[]>([]);

	function handleFiles(files: FileList | null): void {
		selectedFiles = files ? Array.from(files, (file) => file.name) : [];
		onFilesChange?.(files ? Array.from(files) : []);
	}

	function handleDrop(event: DragEvent): void {
		event.preventDefault();
		dragging = false;
		if (type !== "file" || input.disabled) return;
		if (!event.dataTransfer?.files.length) return;
		if (!ref) return;
		const transfer = new DataTransfer();
		for (const file of Array.from(event.dataTransfer.files).slice(0, ref.multiple ? undefined : 1)) transfer.items.add(file);
		ref.files = transfer.files;
		ref.dispatchEvent(new Event("change", { bubbles: true }));
	}
	const describedBy = $derived.by(() => {
		const ids = [
			input.hasDescription ? input.descriptionId : undefined,
			input.invalid ? input.errorId : undefined,
			...(consumerDescribedBy?.split(/\s+/) ?? [])
		].filter((candidate): candidate is string => Boolean(candidate));

		return [...new Set(ids)].join(" ") || undefined;
	});
</script>

{#if type === "file"}
	<div
		class="file-control"
		role="group"
		aria-label="File drop area"
		data-input-control
		data-state={input.state}
		data-dragging={dragging ? "" : undefined}
		ondragenter={(event) => {
			if (!input.disabled && event.dataTransfer?.types.includes("Files")) dragging = true;
		}}
		ondragleave={(event) => {
			if (!event.currentTarget.contains(event.relatedTarget as Node | null)) dragging = false;
		}}
		ondragover={(event) => {
			if (!input.disabled && event.dataTransfer?.types.includes("Files")) event.preventDefault();
		}}
		ondrop={handleDrop}
	>
		<input
			{...restProps}
			bind:this={ref}
			id={input.controlId}
			type="file"
			disabled={input.disabled}
			required={input.required}
			class={["input-control", "input-control--file", className]}
			aria-invalid={input.invalid ? "true" : undefined}
			aria-describedby={describedBy}
			data-state={input.state}
			onchange={(event) => {
				handleFiles(event.currentTarget.files);
				onchange?.(event);
			}}
		/>
		<span class="file-drop-hint" aria-hidden="true">or drop a file here</span>
		{#if selectedFiles.length}
			<span class="file-selected" aria-live="polite">
				{selectedFiles.length === 1 ? "Selected file:" : "Selected files:"}
				{selectedFiles.join(", ")}
			</span>
		{/if}
	</div>
{:else}
	<input
		{...restProps}
		bind:this={ref}
		bind:value
		id={input.controlId}
		{type}
		disabled={input.disabled}
		required={input.required}
		class={["input-control", className]}
		aria-invalid={input.invalid ? "true" : undefined}
		aria-describedby={describedBy}
		data-input-control
		data-state={input.state}
		data-value-kind={valueKind}
		{onchange}
	/>
{/if}

<style>
	.input-control {
		box-sizing: border-box;
		width: 100%;
		min-width: 0;
		min-height: 3.5rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		padding: 0.875rem 1rem;
		color: var(--reading-ink);
		font-family: var(--font-interface);
		font-size: 1rem;
		line-height: 1.5;
		transition:
			border-color 150ms ease,
			background-color 150ms ease;
	}

	.input-control::placeholder {
		color: var(--marginal-note);
		opacity: 1;
	}

	.input-control:hover:not(:disabled):not(:focus-visible) {
		border-color: var(--reading-ink);
	}

	.input-control[aria-invalid="true"] {
		border-color: var(--time-marker);
		border-width: 2px;
		padding: calc(0.875rem - 1px) calc(1rem - 1px);
	}

	.input-control:disabled {
		cursor: not-allowed;
		opacity: 0.62;
	}

	.input-control:read-only:not(:disabled) {
		background: color-mix(in srgb, var(--archive-layer) 72%, var(--reading-room));
	}

	.input-control[data-value-kind="record"] {
		font-family: var(--font-record);
		font-variant-numeric: tabular-nums;
	}
	.file-control {
		display: flex;
		min-width: 0;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.5rem 1rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		padding: 0.75rem;
	}
	.file-control[data-dragging] {
		border: 2px solid var(--register-mark);
		background: color-mix(in srgb, var(--register-mark) 9%, var(--archive-layer));
	}
	.file-control[data-state="invalid"] {
		border: 2px solid var(--time-marker);
	}
	.file-control:focus-within {
		outline: 3px solid var(--register-mark);
		outline-offset: 2px;
	}
	.input-control--file {
		width: auto;
		flex: 1 1 14rem;
		min-height: 44px;
		border: 0;
		background: transparent;
		padding: 0;
		font-size: 0.875rem;
	}
	.input-control--file::file-selector-button {
		min-height: 44px;
		margin-inline-end: 1rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--reading-room);
		color: var(--reading-ink);
		padding: 0.5rem 1rem;
		font: 600 0.875rem var(--font-interface);
		cursor: pointer;
	}
	.input-control--file[aria-invalid="true"] {
		border: 0;
		padding: 0;
	}
	.file-drop-hint {
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
	.file-selected {
		width: 100%;
		color: var(--reading-ink);
		font-size: 0.875rem;
		line-height: 1.43;
		overflow-wrap: anywhere;
	}

	@media (prefers-reduced-motion: reduce) {
		.input-control {
			transition: none;
		}
	}
</style>
